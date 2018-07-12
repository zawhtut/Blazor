// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Blazor.Browser.Rendering;
using Microsoft.AspNetCore.Blazor.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Blazor.Server.Circuits
{
    internal class DefaultCircuitFactory : CircuitFactory
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public DefaultCircuitFactory(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

            StartupActions = new Dictionary<PathString, Action<BrowserRenderer>>();
        }

        public Dictionary<PathString, Action<BrowserRenderer>> StartupActions { get; }

        public override CircuitHost CreateCircuitHost(HttpContext httpContext, IClientProxy client)
        {
            if (!StartupActions.TryGetValue(httpContext.Request.Path, out var config))
            {
                var message = $"Could not find a Blazor startup action for request path {httpContext.Request.Path}";
                throw new InvalidOperationException(message);
            }

            var scope = _scopeFactory.CreateScope();
            var renderer = new BrowserRenderer(scope.ServiceProvider);
            var jsRuntime = new RemoteJSRuntime(client);
            var synchronizationContext = new CircuitSynchronizationContext();

            var circuitHost = new CircuitHost(scope, renderer, config, jsRuntime, synchronizationContext);

            // Initialize per-circuit data that services need
            (circuitHost.Services.GetRequiredService<IJSRuntimeAccessor>() as DefaultJSRuntimeAccessor).JSRuntime = jsRuntime;
            (circuitHost.Services.GetRequiredService<ICircuitAccessor>() as DefaultCircuitAccessor).Circuit = circuitHost.Circuit;

            return circuitHost;
        }
    }
}
