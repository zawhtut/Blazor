// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Browser.Interop;
using Microsoft.AspNetCore.Blazor.Components;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNetCore.Blazor.Bootstrap
{
    /// <summary>
    /// The base class for Blazor Bootstrap components.
    /// </summary>
    public abstract class BootstrapComponentBase : BlazorComponent
    {
        /// <summary>
        /// A dictionary holding any parameter name/value pairs that do not match
        /// properties declared on the component.
        /// </summary>
        protected IDictionary<string, object> UnknownParameters { get; }
            = new Dictionary<string, object>();

        /// <inheritdoc />
        public override void SetParameters(ParameterCollection parameters)
        {
            UnknownParameters.Clear();

            foreach (var param in parameters)
            {
                if (TryGetPropertyInfo(param.Name, out var declaredPropertyInfo))
                {
                    declaredPropertyInfo.SetValue(this, param.Value);
                }
                else
                {
                    UnknownParameters[param.Name] = param.Value;
                }
            }

            StateHasChanged();
        }

        /// <summary>
        /// Invokes the specified JavaScript function in the Bootstrap wrapper library.
        /// </summary>
        protected void InvokeOwnJs(string functionName, params object[] args)
        {
            RegisteredFunction.Invoke<object>($"Microsoft.AspNetCore.Blazor.Bootstrap.{functionName}", args);
        }

        private bool TryGetPropertyInfo(string propertyName, out PropertyInfo result)
        {
            result = GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            return result != null;
        }
    }
}
