// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Blazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Interop = Microsoft.AspNetCore.Blazor.Browser.Services.BrowserUriHelperInterop;

namespace Microsoft.AspNetCore.Blazor.Server.Circuits
{
    /// <summary>
    /// A Server-Side Blazor implemenation of <see cref="IUriHelper"/>.
    /// </summary>
    public class RemoteUriHelper : IUriHelper
    {
        /// <summary>
        /// An event that fires when the navigation location has changed.
        /// </summary>
        public event EventHandler<string> OnLocationChanged;

        private readonly IJSRuntime _jsRuntime;
        private string _uriAbsolute;

        // These two are always kept in sync. We store both representations to
        // avoid having to convert between them on demand.
        private Uri _baseUriWithTrailingSlash;
        private string _baseUriStringWithTrailingSlash;

        /// <summary>
        /// Creates a new <see cref="RemoteUriHelper"/>.
        /// </summary>
        /// <param name="jsRuntime"></param>
        public RemoteUriHelper(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        /// <summary>
        /// Initializes the <see cref="RemoteUriHelper"/>.
        /// </summary>
        /// <param name="uriAbsolute">The absolute URI of the current page.</param>
        /// <param name="baseUriAbsolute">The absolute base URI of the current page.</param>
        public void Initialize(string uriAbsolute, string baseUriAbsolute)
        {
            _baseUriStringWithTrailingSlash = ToBaseUri(baseUriAbsolute);
            _baseUriWithTrailingSlash = new Uri(_baseUriStringWithTrailingSlash);

            _uriAbsolute = uriAbsolute;

            _jsRuntime.InvokeAsync<object>(
                Interop.EnableNavigationInterception,
                typeof(RemoteUriHelper).Assembly.GetName().Name,
                nameof(NotifyLocationChanged));
        }

        /// <summary>
        /// For framework use only.
        /// </summary>
        [JSInvokable(nameof(NotifyLocationChanged))]
        public static void NotifyLocationChanged(string uriAbsolute)
        {
            var circuit = Circuit.Current;
            if (circuit == null)
            {
                var message = $"{nameof(NotifyLocationChanged)} called without a circuit.";
                throw new InvalidOperationException(message);
            }

            var uriHelper = (RemoteUriHelper)circuit.Services.GetRequiredService<IUriHelper>();

            uriHelper._uriAbsolute = uriAbsolute;
            uriHelper.OnLocationChanged?.Invoke(uriHelper, uriAbsolute);
        }

        /// <summary>
        /// Gets the current absolute URI.
        /// </summary>
        /// <returns>The current absolute URI.</returns>
        public string GetAbsoluteUri() => _uriAbsolute;

        /// <summary>
        /// Gets the base URI (with trailing slash) that can be prepended before relative URI paths to produce an absolute URI.
        /// Typically this corresponds to the 'href' attribute on the document's &lt;base&gt; element.
        /// </summary>
        /// <returns>The URI prefix, which has a trailing slash.</returns>
        public string GetBaseUri() => _baseUriStringWithTrailingSlash;

        /// <summary>
        /// Navigates to the specified URI.
        /// </summary>
        /// <param name="uri">The destination URI. This can be absolute, or relative to the base URI
        /// (as returned by <see cref="GetBaseUri"/>).</param>
        public void NavigateTo(string uri)
        {
            _jsRuntime.InvokeAsync<object>(Interop.NavigateTo, uri);
        }

        /// <summary>
        /// Converts a relative URI into an absolute one (by resolving it
        /// relative to the current absolute URI).
        /// </summary>
        /// <param name="href">The relative URI.</param>
        /// <returns>The absolute URI.</returns>
        public Uri ToAbsoluteUri(string href) => new Uri(_baseUriWithTrailingSlash, href);

        /// <summary>
        /// Given a base URI (e.g., one previously returned by <see cref="GetBaseUri"/>),
        /// converts an absolute URI into one relative to the base URI prefix.
        /// </summary>
        /// <param name="baseUri">The base URI prefix (e.g., previously returned by <see cref="GetBaseUri"/>).</param>
        /// <param name="locationAbsolute">An absolute URI that is within the space of the base URI.</param>
        /// <returns>A relative URI path.</returns>
        public string ToBaseRelativePath(string baseUri, string locationAbsolute)
        {
            if (locationAbsolute.StartsWith(baseUri, StringComparison.Ordinal))
            {
                // The absolute URI must be of the form "{baseUri}something" (where
                // baseUri ends with a slash), and from that we return "something"
                return locationAbsolute.Substring(baseUri.Length);
            }
            else if ($"{locationAbsolute}/".Equals(baseUri, StringComparison.Ordinal))
            {
                // Special case: for the base URI "/something/", if you're at
                // "/something" then treat it as if you were at "/something/" (i.e.,
                // with the trailing slash). It's a bit ambiguous because we don't know
                // whether the server would return the same page whether or not the
                // slash is present, but ASP.NET Core at least does by default when
                // using PathBase.
                return string.Empty;
            }

            throw new ArgumentException($"The URI '{locationAbsolute}' is not contained by the base URI '{baseUri}'.");
        }

        private static string ToBaseUri(string absoluteBaseUri)
        {
            if (absoluteBaseUri != null)
            {
                var lastSlashIndex = absoluteBaseUri.LastIndexOf('/');
                if (lastSlashIndex >= 0)
                {
                    return absoluteBaseUri.Substring(0, lastSlashIndex + 1);
                }
            }

            return "/";
        }
    }
}
