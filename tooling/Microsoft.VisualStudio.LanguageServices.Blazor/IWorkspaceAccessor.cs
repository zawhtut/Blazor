// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServices.Blazor
{
	// VS for Mac doesn't use a single 'main' workspace so we need to query workspaces
    // per document
	internal interface IWorkspaceAccessor
    {
		Workspace GetWorkspace(ITextView textView);
    }
}
