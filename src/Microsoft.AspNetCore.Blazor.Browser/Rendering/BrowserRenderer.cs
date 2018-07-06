// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Browser.Services;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.Rendering;
using Microsoft.JSInterop;
using Mono.WebAssembly.Interop;
using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Blazor.Browser.Rendering
{
    /// <summary>
    /// Provides mechanisms for rendering <see cref="IComponent"/> instances in a
    /// web browser, dispatching events to them, and refreshing the UI as required.
    /// </summary>
    public class BrowserRenderer : Renderer, IDisposable
    {
        private readonly int _browserRendererId;

        /// <summary>
        /// Notifies when a rendering exception occured.
        /// </summary>
        public event EventHandler<Exception> OnException;

        /// <summary>
        /// Constructs an instance of <see cref="BrowserRenderer"/>.
        /// </summary>
        public BrowserRenderer(): this(new BrowserServiceProvider())
        {
        }

        /// <summary>
        /// Constructs an instance of <see cref="BrowserRenderer"/>.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to use when initializing components.</param>
        public BrowserRenderer(IServiceProvider serviceProvider): base(serviceProvider)
        {
            _browserRendererId = BrowserRendererRegistry.Add(this);
        }

        internal void DispatchBrowserEvent(int componentId, int eventHandlerId, UIEventArgs eventArgs)
            => DispatchEvent(componentId, eventHandlerId, eventArgs);

        /// <summary>
        /// Attaches a new root component to the renderer,
        /// causing it to be displayed in the specified DOM element.
        /// </summary>
        /// <typeparam name="TComponent">The type of the component.</typeparam>
        /// <param name="domElementSelector">A CSS selector that uniquely identifies a DOM element.</param>
        public void AddComponent<TComponent>(string domElementSelector)
            where TComponent: IComponent
        {
            AddComponent(typeof(TComponent), domElementSelector);
        }

        /// <summary>
        /// Associates the <see cref="IComponent"/> with the <see cref="BrowserRenderer"/>,
        /// causing it to be displayed in the specified DOM element.
        /// </summary>
        /// <param name="componentType">The type of the component.</param>
        /// <param name="domElementSelector">A CSS selector that uniquely identifies a DOM element.</param>
        public void AddComponent(Type componentType, string domElementSelector)
        {
            var component = InstantiateComponent(componentType);
            var componentId = AssignComponentId(component);

            var attachComponentTask = JSRuntime.Current.InvokeAsync<object>(
                "Blazor._internal.attachRootComponentToElement",
                _browserRendererId,
                domElementSelector,
                componentId);
            CaptureAsyncExceptions(attachComponentTask);

            component.SetParameters(ParameterCollection.Empty);
        }

        /// <summary>
        /// Disposes the instance.
        /// </summary>
        public void Dispose()
        {
            BrowserRendererRegistry.TryRemove(_browserRendererId);
        }

        /// <inheritdoc />
        protected override void UpdateDisplay(in RenderBatch batch)
        {
            if (JSRuntime.Current is MonoWebAssemblyJSRuntime mono)
            {
                mono.InvokeUnmarshalled<int, RenderBatch, object>(
                    "Blazor._internal.renderBatch",
                    _browserRendererId,
                    batch);
            }
            else if (JSRuntime.Current is IRenderBatchDispatcher renderBatchDispatcher)
            {
                var task = renderBatchDispatcher.RenderBatchAsync(_browserRendererId, batch);
                CaptureAsyncExceptions(task);
            }
            else
            {
                throw new InvalidOperationException($"The current {nameof(IJSRuntime)} does not provide an implementation of {nameof(IRenderBatchDispatcher)}.");
            }
        }

        private void CaptureAsyncExceptions(Task task)
        {
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    OnException?.Invoke(this, t.Exception);
                }
            });
        }
    }
}
