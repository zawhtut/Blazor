// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Browser.Rendering;
using Microsoft.AspNetCore.Blazor.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Blazor.Server.Bots
{
    internal class RemoteJSRuntime : JSRuntimeBase, IRenderBatchDispatcher
    {
        private readonly IClientProxy _clientProxy;

        public RemoteJSRuntime(IClientProxy clientProxy)
        {
            _clientProxy = clientProxy ?? throw new ArgumentNullException(nameof(clientProxy));
        }

        public Task RenderBatchAsync(int browserRendererId, RenderBatch batch)
        {
            return _clientProxy.SendAsync("JS.RenderBatch", browserRendererId, batch);
        }

        protected override void BeginInvokeJS(long asyncHandle, string identifier, string argsJson)
        {
            _clientProxy.SendAsync("JS.BeginInvokeJS", asyncHandle, identifier, argsJson);
        }
    }
}
