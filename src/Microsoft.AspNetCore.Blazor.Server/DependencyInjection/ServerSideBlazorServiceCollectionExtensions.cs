// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Blazor;
using Microsoft.AspNetCore.Blazor.Server.Circuits;
using Microsoft.AspNetCore.Blazor.Services;
using Microsoft.JSInterop;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods to configure an <see cref="IServiceCollection"/> for Server-Side Blazor.
    /// </summary>
    public static class ServerSideBlazorServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Server-Side Blazor services to the service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddServerSideBlazor(this IServiceCollection services)
        {
            return AddServerSideBlazor(services, null);
        }

        /// <summary>
        /// Adds Server-Side Blazor services to the service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configure">A delegate to configure the <see cref="ServerSideBlazorOptions"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddServerSideBlazor(this IServiceCollection services, Action<ServerSideBlazorOptions> configure)
        {
            services.AddSingleton<CircuitFactory, DefaultCircuitFactory>();
            services.AddScoped<ICircuitAccessor, DefaultCircuitAccessor>();
            services.AddScoped<Circuit>(s => s.GetRequiredService<ICircuitAccessor>().Circuit);

            services.AddScoped<IJSRuntimeAccessor, DefaultJSRuntimeAccessor>();
            services.AddScoped<IJSRuntime>(s => s.GetRequiredService<IJSRuntimeAccessor>().JSRuntime);

            services.AddScoped<IUriHelper, RemoteUriHelper>();

            services.AddSignalR().AddMessagePackProtocol(options =>
            {
                // TODO: Enable compression, either by having SignalR use
                // LZ4MessagePackSerializer instead of MessagePackSerializer,
                // or perhaps by compressing the RenderBatch bytes ourselves
                // and then using https://github.com/nodeca/pako in JS to decompress.
                options.FormatterResolvers.Insert(0, new RenderBatchFormatterResolver());
            });

            if (configure != null)
            {
                services.Configure(configure);
            }

            return services;
        }
    }
}
