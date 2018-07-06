// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Services;
using Microsoft.JSInterop;
using System;

namespace Microsoft.AspNetCore.Blazor.Server.Bots
{
    // TODO: Stop duplicating all this implementation.
    // Maybe have a UriHelperBase abstract base class

    internal class RemoteUriHelper : IUriHelper
    {
        const string _functionPrefix = "Blazor._internal.uriHelper.";
        private readonly IJSRuntime _jsRuntime;
        private string _uriAbsolute;

        // These two are always kept in sync. We store both representations to
        // avoid having to convert between them on demand.
        static Uri _baseUriWithTrailingSlash;
        static string _baseUriStringWithTrailingSlash;

        public RemoteUriHelper(IJSRuntime jsRuntime, string initialUriAbsolute, string baseUriAbsolute)
        {
            _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));

            _baseUriStringWithTrailingSlash = ToBaseUri(baseUriAbsolute);
            _baseUriWithTrailingSlash = new Uri(_baseUriStringWithTrailingSlash);

            _uriAbsolute = initialUriAbsolute;

            JSRuntime.Current.InvokeAsync<object>(_functionPrefix + "enableNavigationInterception");
        }

#pragma warning disable CS0067
        public event EventHandler<string> OnLocationChanged;
#pragma warning restore CS0067

        public string GetAbsoluteUri() => _uriAbsolute;

        public string GetBaseUri() => _baseUriStringWithTrailingSlash;

        public void NavigateTo(string uri)
        {
            JSRuntime.Current.InvokeAsync<object>(_functionPrefix + "navigateTo", uri);
        }

        public Uri ToAbsoluteUri(string relativeUri)
            => new Uri(_baseUriWithTrailingSlash, relativeUri);

        public string ToBaseRelativePath(string baseUri, string absoluteUri)
        {
            if (absoluteUri.StartsWith(baseUri, StringComparison.Ordinal))
            {
                // The absolute URI must be of the form "{baseUri}something" (where
                // baseUri ends with a slash), and from that we return "something"
                return absoluteUri.Substring(baseUri.Length);
            }
            else if ($"{absoluteUri}/".Equals(baseUri, StringComparison.Ordinal))
            {
                // Special case: for the base URI "/something/", if you're at
                // "/something" then treat it as if you were at "/something/" (i.e.,
                // with the trailing slash). It's a bit ambiguous because we don't know
                // whether the server would return the same page whether or not the
                // slash is present, but ASP.NET Core at least does by default when
                // using PathBase.
                return string.Empty;
            }

            throw new ArgumentException($"The URI '{absoluteUri}' is not contained by the base URI '{baseUri}'.");
        }

        internal void NotifyLocationChanged(string newAbsoluteUri)
        {
            _uriAbsolute = newAbsoluteUri;
            OnLocationChanged?.Invoke(null, newAbsoluteUri);
        }

        static string ToBaseUri(string absoluteBaseUri)
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
