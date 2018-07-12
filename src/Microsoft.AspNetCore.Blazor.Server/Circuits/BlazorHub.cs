// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Blazor.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Blazor.Server.Circuits
{
    internal class BlazorHub : Hub
    {
        private static readonly object CircuitKey = new object();

        private readonly CircuitFactory _circuitFactory;

        public BlazorHub(CircuitFactory circuitFactory)
        {
            _circuitFactory = circuitFactory ?? throw new ArgumentNullException(nameof(circuitFactory));
        }

        public CircuitHost CircuitHost
        {
            get => (CircuitHost)Context.Items[CircuitKey];
            set => Context.Items[CircuitKey] = value;
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            CircuitHost.Dispose();
            return base.OnDisconnectedAsync(exception);
        }

        public async Task StartCircuit(string uriAbsolute, string baseUriAbsolute)
        {
            var circuitHost = _circuitFactory.CreateCircuitHost(Context.GetHttpContext(), Clients.Caller);

            var uriHelper = (RemoteUriHelper)circuitHost.Services.GetRequiredService<IUriHelper>();
            if (uriHelper != null)
            {
                uriHelper.Initialize(uriAbsolute, baseUriAbsolute);
            }

            await circuitHost.InitializeAsync();
            CircuitHost = circuitHost;
        }

        public void BeginInvokeDotNetFromJS(string callId, string assemblyName, string methodIdentifier, string argsJson)
        {
            CircuitHost.BeginInvokeDotNetFromJS(callId, assemblyName, methodIdentifier, argsJson);
        }
    }
}
