// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Blazor.Bootstrap
{
    /// <summary>
    /// Specifies the type of an alert.
    /// </summary>
    public enum AlertType
    {
        /// <summary>
        /// A primary alert.
        /// </summary>
        Primary,

        /// <summary>
        /// A secondary alert.
        /// </summary>
        Secondary,

        /// <summary>
        /// An alert indicating success.
        /// </summary>
        Success,

        /// <summary>
        /// An alert indicating danger.
        /// </summary>
        Danger,

        /// <summary>
        /// An alert indicating a warning.
        /// </summary>
        Warning,

        /// <summary>
        /// An alert representing information.
        /// </summary>
        Info,

        /// <summary>
        /// An alert with light styling.
        /// </summary>
        Light,

        /// <summary>
        /// An alert with dark styling.
        /// </summary>
        Dark,
    }
}
