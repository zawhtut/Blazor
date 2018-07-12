// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNetCore.Blazor.Rendering;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Blazor.Server.Circuits
{
    /// <summary>
    /// Represents an active connection between a Blazor server and a client.
    /// </summary>
    public class Circuit
    {
        private static AsyncLocal<Circuit> _current  = new AsyncLocal<Circuit>();

        /// <summary>
        /// Gets the current <see cref="Circuit"/>, if any.
        /// </summary>
        public static Circuit Current => _current.Value;

        /// <summary>
        /// Sets the current <see cref="Circuit"/>.
        /// </summary>
        /// <param name="circuit">The <see cref="Circuit"/>.</param>
        /// <remarks>
        /// Calling <see cref="SetCurrentCircuit(Circuit)"/> will store the circuit
        /// and other related values such as the <see cref="IJSRuntime"/> and <see cref="Renderer"/>
        /// in the local execution context. Application code should not need to call this method,
        /// it is primarily used by the Server-Side Blazor infrastructure.
        /// </remarks>
        public static void SetCurrentCircuit(Circuit circuit)
        {
            if (circuit == null)
            {
                throw new ArgumentNullException(nameof(circuit));
            }

            _current.Value = circuit;

            Microsoft.JSInterop.JSRuntime.SetCurrentJSRuntime(circuit.JSRuntime);
        }
        
        internal Circuit(CircuitHost circuitHost)
        {
            JSRuntime = circuitHost.JSRuntime;
        }

        /// <summary>
        /// Gets the <see cref="IJSRuntime"/> associated with this circuit.
        /// </summary>
        public IJSRuntime JSRuntime { get; }
    }
}
