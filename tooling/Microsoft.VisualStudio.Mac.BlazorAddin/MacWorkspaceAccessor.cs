// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Blazor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using MonoDevelop.Ide;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using Workspace = Microsoft.CodeAnalysis.Workspace;

namespace Microsoft.VisualStudio.Mac.BlazorAddin
{
	[Export(typeof(IWorkspaceAccessor))]
	internal class MacWorkspaceAccessor : IWorkspaceAccessor
	{
        private readonly ITextDocumentFactoryService _documentFactory;

        [ImportingConstructor]
		public MacWorkspaceAccessor(ITextDocumentFactoryService documentFactory)
        {
            if (documentFactory == null)
            {
                throw new ArgumentNullException(nameof(documentFactory));
            }

            _documentFactory = documentFactory;
        }

		public Workspace GetWorkspace(ITextView textView)
		{
			if (textView == null)
			{
				throw new ArgumentNullException(nameof(textView));
			}

			var project = GetProject(textView);
			var solution = project?.ParentSolution;
            if (solution == null)
            {
				return null;
            }
         
			var workspace = TypeSystemService.GetWorkspace(solution);

            // Workspace cannot be null at this point. If TypeSystemService.GetWorkspace isn't able to find a corresponding
            // workspace it returns an empty workspace. Therefore, in order to see if we have a valid workspace we need to
            // cross-check it against the list of active non-empty workspaces.

            if (!TypeSystemService.AllWorkspaces.Contains(workspace))
            {
				// We were returned the empty workspace which is equivalent to us not finding a valid workspace for our text buffer.
				return null;
            }

			return workspace;
		}

        private DotNetProject GetProject(ITextView textView)
		{
			// If there's no document we can't find the FileName, or look for an associated project.
            if (!_documentFactory.TryGetTextDocument(textView.TextBuffer, out var textDocument))
            {
                return null;
            }

            var projectsContainingFilePath = IdeApp.Workspace.GetProjectsContainingFile(textDocument.FilePath);
            foreach (var project in projectsContainingFilePath)
            {	
                if (!(project is DotNetProject))
                {
                    continue;
                }

                var projectFile = project.GetProjectFile(textDocument.FilePath);
                if (!projectFile.IsHidden)
                {
                    return (DotNetProject)project;
                }
            }

			return null;
		}
	}
}
