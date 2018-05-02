// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Server;
using Microsoft.AspNetCore.Blazor.Server.AutoRebuild;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Builder
{
    internal static class AutoRebuildExtensions
    {
        // Note that we don't need to watch typical static-file extensions (.css, .js, etc.)
        // because anything in wwwroot is just served directly from disk on each reload. But
        // as a special case, we do watch index.html because it needs compilation.
        // TODO: Make the set of extensions and exclusions configurable in csproj
        private static string[] _includedSuffixes = new[] { ".cs", ".cshtml", "index.html" };
        private static string[] _excludedDirectories = new[] { "obj", "bin" };

        public static void UseAutoRebuild(this IApplicationBuilder appBuilder, BlazorConfig config)
        {
            // Currently this only supports VS for Windows. Later on we can add
            // an IRebuildService implementation for VS for Mac, etc.
            if (!VSForWindowsRebuildService.TryCreate(out var rebuildService))
            {
                return; // You're not on Windows, or you didn't launch this process from VS
            }

            // Assume we're up to date when the app starts.
            var buildToken = new RebuildToken(new DateTime(1970, 1, 1)) { BuildTask = Task.CompletedTask, };

            WatchFileSystem(config, () =>
            {
                // Don't start the recompilation immediately. We only start it when the next
                // HTTP request arrives, because it's annoying if the IDE is constantly rebuilding
                // when you're making changes to multiple files and aren't ready to reload
                // in the browser yet.
                //
                // Replacing the token means that new requests that come in will trigger a rebuild,
                // and will all 'join' that build until a new file change occurs.
                buildToken = new RebuildToken(DateTime.Now);
            });

            appBuilder.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/_awaitbuild"))
                {
                    // Here we know we have to join an existing in-process build.
                    // IDEA: During this phase, we could get back realtime build output and stream
                    // it to the browser. That would not only give the dev something to look at, but
                    // (at least partially) solves how to get build failure info to them.
                    var since = new DateTime(long.Parse(context.Request.Query["since"]));
                    await rebuildService.PerformRebuildAsync(
                            config.SourceMSBuildPath,
                            since);
                    context.Response.Redirect("/"); // TODO: Redirect to originally-requested URL
                    return;
                }

                try
                {
                    var token = buildToken;
                    if (token.BuildTask == null)
                    {
                        // SUMMARY: When we're going to trigger a build, don't try to keep the request
                        // hanging open, because VS will kill the server. Instead, return some HTML that
                        // will make the browser start a new request, because that's OK. Although it looks
                        // like there's a race condition here (what if the new request starts before VS has
                        // killed the old server?) it doesn't appear there really is, because the browser
                        // won't start acting on the meta-refresh until the response has finished arriving,
                        // which won't happen until VS has killed the old server.
                        await context.Response.WriteAsync(@"
                        <html><head>
                            <meta http-equiv=""refresh"" content=""0;url=/_awaitbuild?since=" + token.LastChange.Ticks + @""">
                        </head><body>building</body></html>
                        ");

                        // The build is out of date, but a new build is not yet started.
                        //
                        // We can count on VS to only allow one build at a time, this is a safe race
                        // because if we request a second concurrent build, it will 'join' the current one.
                        var task = rebuildService.PerformRebuildAsync(
                            config.SourceMSBuildPath,
                            token.LastChange);
                        token.BuildTask = task;
                    }

                    // In the general case it's safe to await this task, it will be a completed task
                    // if everything is up to date.
                    await token.BuildTask;
                }
                catch (Exception)
                {
                    // If there's no listener on the other end of the pipe, or if anything
                    // else goes wrong, we just let the incoming request continue.
                    // There's nowhere useful to log this information so if people report
                    // problems we'll just have to get a repro and debug it.
                    // If it was an error on the VS side, it logs to the output window.
                }

                await next();
            });
        }

        private static void WatchFileSystem(BlazorConfig config, Action onWrite)
        {
            var clientAppRootDir = Path.GetDirectoryName(config.SourceMSBuildPath);
            var excludePathPrefixes = _excludedDirectories.Select(subdir
                => Path.Combine(clientAppRootDir, subdir) + Path.DirectorySeparatorChar);

            var fsw = new FileSystemWatcher(clientAppRootDir);
            fsw.Created += OnEvent;
            fsw.Changed += OnEvent;
            fsw.Deleted += OnEvent;
            fsw.Renamed += OnEvent;
            fsw.IncludeSubdirectories = true;
            fsw.EnableRaisingEvents = true;

            void OnEvent(object sender, FileSystemEventArgs eventArgs)
            {
                if (!File.Exists(eventArgs.FullPath))
                {
                    // It's probably a directory rather than a file
                    return;
                }

                if (!_includedSuffixes.Any(ext => eventArgs.Name.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    // Not a candiate file type
                    return;
                }

                if (excludePathPrefixes.Any(prefix => eventArgs.FullPath.StartsWith(prefix, StringComparison.Ordinal)))
                {
                    // In an excluded subdirectory
                    return;
                }

                onWrite();
            }
        }

        // Represents a three-state value for the state of the build
        //
        // BuildTask == null means the build is out of date, but no build has started
        // BuildTask.IsCompleted == false means the build has been started, but has not completed
        // BuildTask.IsCompleted == true means the build has completed
        private class RebuildToken
        {
            public RebuildToken(DateTime lastChange)
            {
                LastChange = lastChange;
            }

            public DateTime LastChange { get; }

            public Task BuildTask;
        }
    }
}
