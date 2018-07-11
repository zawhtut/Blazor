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

            StartupActions = new List<(PathString path, Action<BrowserRenderer> config)>();
        }

        public List<(PathString path, Action<BrowserRenderer> config)> StartupActions { get; }

        public override CircuitHost CreateCircuit(HttpContext httpContext, IClientProxy client)
        {
            Action<BrowserRenderer> config = null;
            for (var i = 0; i < StartupActions.Count; i++)
            {
                if (httpContext.Request.Path.StartsWithSegments(StartupActions[i].path))
                {
                    config = StartupActions[i].config;
                }
            }

            if (config == null)
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
