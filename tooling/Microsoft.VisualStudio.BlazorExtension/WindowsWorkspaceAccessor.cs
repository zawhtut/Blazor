// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Blazor;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Mac.BlazorAddin
{
    [Export(typeof(IWorkspaceAccessor))]
    internal class WindowsWorkspaceAccessor : IWorkspaceAccessor
    {
        private readonly Workspace _workspace;

        [ImportingConstructor]
        public WindowsWorkspaceAccessor(VisualStudioWorkspace workspace)
        {
            if (documentFactory == null)
            {
                throw new ArgumentNullException(nameof(documentFactory));
            }

            _workspace = workspace;
        }

        public Workspace GetWorkspace(ITextView textView)
        {
            // That was easy.
            return _workspace;
        }
    }
}
