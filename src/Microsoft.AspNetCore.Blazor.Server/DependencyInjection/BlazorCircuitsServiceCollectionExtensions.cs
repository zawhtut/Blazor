// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Blazor;
using Microsoft.AspNetCore.Blazor.Server;
using Microsoft.AspNetCore.Blazor.Server.Circuits;
using Microsoft.AspNetCore.Blazor.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods to configure an <see cref="IServiceCollection"/> for Blazor circuits.
    /// </summary>
    public static class BlazorCircuitsServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Blazor circuits services to the service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddBlazorCircuits(this IServiceCollection services)
        {
            return AddBlazorCircuits(services, null);
        }

        /// <summary>
        /// Adds Blazor circuits services to the service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configure">A delegate to configure the <see cref="BlazorCircuitsOptions"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddBlazorCircuits(this IServiceCollection services, Action<BlazorCircuitsOptions> configure)
        {
            services.AddSingleton<CircuitFactory, DefaultCircuitFactory>();
            services.AddScoped<ICircuitAccessor, DefaultCircuitAccessor>();
            services.AddScoped<IJSRuntimeAccessor, DefaultJSRuntimeAccessor>();
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
