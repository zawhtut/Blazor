// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Blazor.Browser.Rendering;
using Microsoft.AspNetCore.Blazor.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods to configure an <see cref="IApplicationBuilder"/> for Server-Side Blazor.
    /// </summary>
    public static class ServerSideBlazorApplicationBuilderExtensions
    {
        /// <summary>
        /// Registers middleware for Server-Side Blazor.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="startupAction">A delegate used to configure the renderer.</param>
        /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseServerSideBlazor(
            this IApplicationBuilder builder,
            Action<BrowserRenderer> startupAction)
        {
            var endpoint = "/_blazor";

            var factory = (DefaultCircuitFactory)builder.ApplicationServices.GetRequiredService<CircuitFactory>();
            factory.StartupActions.Add(endpoint, startupAction);

            builder.UseSignalR(route => route.MapHub<BlazorHub>(endpoint));

            return builder;
        }
    }
}
