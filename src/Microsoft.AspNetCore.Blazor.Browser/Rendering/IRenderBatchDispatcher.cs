// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Rendering;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Blazor.Browser.Rendering
{
    /// <summary>
    /// Interface implemented by <see cref="IJSRuntime"/> instances that
    /// override the mechanism for dispatching "update display" calls.
    /// </summary>
    public interface IRenderBatchDispatcher : IJSRuntime
    {
        /// <summary>
        /// Renders a batch of UI updates.
        /// </summary>
        /// <param name="rendererId">An identifier for the <see cref="Renderer"/>.</param>
        /// <param name="batch">The <see cref="RenderBatch"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task RenderBatchAsync(int rendererId, RenderBatch batch);
    }
}
