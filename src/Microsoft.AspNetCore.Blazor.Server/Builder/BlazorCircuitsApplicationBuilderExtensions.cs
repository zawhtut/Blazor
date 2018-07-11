// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Blazor.Browser.Rendering;
using Microsoft.AspNetCore.Blazor.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods to configure an <see cref="IApplicationBuilder"/> for Blazor circuits.
    /// </summary>
    public static class BlazorCircuitsApplicationBuilderExtensions
    {
        /// <summary>
        /// Temporary vague approximation to server-side execution needed so I can
        /// build the rest of the interop.
        /// </summary>  
        public static IApplicationBuilder UseBlazorOnTheServer(
            this IApplicationBuilder builder,
            Action<BrowserRenderer> startupAction)
        {
            var endpoint = "/_blazor";

            var factory = builder.ApplicationServices.GetRequiredService<CircuitFactory>() as DefaultCircuitFactory;
            if (factory != null)
            {
                factory.StartupActions.Add((endpoint, startupAction));
            }

            builder.UseSignalR(route => route.MapHub<BlazorHub>(endpoint));

            return builder;
        }
    }
}
