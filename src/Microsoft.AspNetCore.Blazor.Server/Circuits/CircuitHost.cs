// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Blazor.Browser.Rendering;
using Microsoft.AspNetCore.Blazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Blazor.Server.Circuits
{
    internal class CircuitHost : IDisposable
    {
        public event UnhandledExceptionEventHandler UnhandledException;

        private bool _isInitialized;
        private Action<BrowserRenderer> _configureRenderer;

        public CircuitHost(
            IServiceScope scope,
            BrowserRenderer renderer,
            Action<BrowserRenderer> configureRenderer,
            IJSRuntime jsRuntime,
            CircuitSynchronizationContext synchronizationContext)
        {
            Scope = scope ?? throw new ArgumentNullException(nameof(scope));
            Renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            _configureRenderer = configureRenderer ?? throw new ArgumentNullException(nameof(configureRenderer));
            JSRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
            SynchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));

            Services = scope.ServiceProvider;

            Circuit = new Circuit(this);
        }

        public Circuit Circuit { get; }

        public IJSRuntime JSRuntime { get; }

        public BrowserRenderer Renderer { get; }

        public IServiceScope Scope { get; }

        public IServiceProvider Services { get; }
        
        public CircuitSynchronizationContext SynchronizationContext { get; }

        public async Task InitializeAsync()
        {
            await SynchronizationContext.Invoke(() =>
            {
                Microsoft.JSInterop.JSRuntime.SetCurrentJSRuntime(JSRuntime);
                _configureRenderer(Renderer);
            });

            _isInitialized = true;
        }

        public async void BeginInvokeDotNetFromJS(string callId, string assemblyName, string methodIdentifier, string argsJson)
        {
            AssertInitialized();

            try
            {
                await SynchronizationContext.Invoke(() =>
                {
                    Microsoft.JSInterop.JSRuntime.SetCurrentJSRuntime(JSRuntime);

                    switch (methodIdentifier)
                    {
                        // Massive hack. Need a common system for dispatching calls within the context of
                        // a specific circuit / service provider / etc. Maybe we just need to have an
                        // asynclocal Circuit.Current, just like JSRuntime.Current, then if the call target
                        // is a static it can obtain the services it needs to invoke instance methods on.
                        case "NotifyLocationChanged":
                            var args = Json.Deserialize<string[]>(argsJson);
                            var uriHelper = (RemoteUriHelper)Services.GetRequiredService<IUriHelper>();
                            uriHelper.NotifyLocationChanged(args[0]);
                            break;
                        default:
                            DotNetDispatcher.BeginInvoke(callId, assemblyName, methodIdentifier, argsJson);
                            break;
                    }
                });
            }
            catch (Exception ex)
            {
                UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(ex, isTerminating: false));
            }
        }

        public void Dispose()
        {
            Scope.Dispose();
        }

        private void AssertInitialized()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Something is calling into the circuit before Initialize() completes");
            }
        }
    }
}
