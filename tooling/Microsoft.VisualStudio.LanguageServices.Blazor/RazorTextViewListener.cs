// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Blazor
{
    [ContentType(RazorLanguage.ContentType)]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    [Export(typeof(ITextViewConnectionListener))]
    internal class BlazorOpenDocumentTracker : ITextViewConnectionListener
    {
        private readonly RazorEditorFactoryService _editorFactory;
        private readonly IWorkspaceAccessor _workspaceAccessor;
        
        private readonly HashSet<ITextView> _openViews;
        private readonly HashSet<Workspace> _workspaces;

        private Type _codeGeneratorType;

        [ImportingConstructor]
        public BlazorOpenDocumentTracker(RazorEditorFactoryService editorFactory, IWorkspaceAccessor workspaceAccessor)
        {
            if (editorFactory == null)
            {
                throw new ArgumentNullException(nameof(editorFactory));
            }

            if (workspaceAccessor == null)
            {
                throw new ArgumentNullException(nameof(workspaceAccessor));
            }

            _editorFactory = editorFactory;
            _workspaceAccessor = workspaceAccessor;

            _openViews = new HashSet<ITextView>();
            _workspaces = new HashSet<Workspace>();
        }
              
        public void SubjectBuffersConnected(ITextView textView, ConnectionReason reason, IReadOnlyCollection<ITextBuffer> subjectBuffers)
        {
            if (textView == null)
            {
                throw new ArgumentException(nameof(textView));
            }

            if (subjectBuffers == null)
            {
                throw new ArgumentNullException(nameof(subjectBuffers));
            }

            _openViews.Add(textView);

            var workspace = _workspaceAccessor.GetWorkspace(textView);
            if (workspace != null && _workspaces.Add(workspace))
            {
                workspace.WorkspaceChanged += Workspace_WorkspaceChanged;
            }
        }

        public void SubjectBuffersDisconnected(ITextView textView, ConnectionReason reason, IReadOnlyCollection<ITextBuffer> subjectBuffers)
        {
            if (textView == null)
            {
                throw new ArgumentException(nameof(textView));
            }

            if (subjectBuffers == null)
            {
                throw new ArgumentNullException(nameof(subjectBuffers));
            }

            _openViews.Remove(textView);

            // We don't bother cleaning up our event registrations on workspaces right now. 
            // There are very few of them, and only one we care about in VS for windows.
        }
        
      
        // We're watching the Roslyn workspace for changes specifically because we want
        // to know when the language service has processed a file change.
        //
        // It might be more elegant to use a file watcher rather than sniffing workspace events
        // but there would be a delay between the file watcher and Roslyn processing the update.
        private void Workspace_WorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            switch (e.Kind)
            {
                case WorkspaceChangeKind.DocumentAdded:
                case WorkspaceChangeKind.DocumentChanged:
                case WorkspaceChangeKind.DocumentInfoChanged:
                case WorkspaceChangeKind.DocumentReloaded:
                case WorkspaceChangeKind.DocumentRemoved:

                    var document = e.NewSolution.GetDocument(e.DocumentId);
                    if (document == null || document.FilePath == null)
                    {
                        break;
                    }

                    if (!document.FilePath.EndsWith(".g.i.cs"))
                    {
                        break;
                    }

                    OnDeclarationsChanged();
                    break;
            }
        }

        private void OnDeclarationsChanged()
        {
            // This is a design-time Razor file change.Go poke all of the open
            // Razor documents and tell them to update.
            var buffers = _openViews
                .SelectMany(v => v.BufferGraph.GetTextBuffers(b => b.ContentType.IsOfType("RazorCSharp")))
                .Distinct()
                .ToArray();

            if (_codeGeneratorType == null)
            {
                try
                {
                    var assembly = Assembly.Load("Microsoft.VisualStudio.Web.Editors.Razor.4_0, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
                    _codeGeneratorType = assembly.GetType("Microsoft.VisualStudio.Web.Editors.Razor.RazorCodeGenerator");
                }
                catch (Exception)
                {
                    // If this fails, just unsubscribe. We won't be able to do our work, so just don't waste time.
                    foreach (var workspace in _workspaces)
                    {
                        workspace.WorkspaceChanged -= Workspace_WorkspaceChanged;
                    }
                }
            }

            foreach (var buffer in buffers)
            {
                try
                {
                    var tryGetFromBuffer = _codeGeneratorType.GetMethod("TryGetFromBuffer", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                    var args = new object[] { buffer, null };
                    if (!(bool)tryGetFromBuffer.Invoke(null, args) || args[1] == null)
                    {
                        continue;
                    }

                    var field = _codeGeneratorType.GetField("_tagHelperDescriptorResolver", BindingFlags.Instance | BindingFlags.NonPublic);
                    var resolver = field.GetValue(args[1]);
                    if (resolver == null)
                    {
                        continue;
                    }

                    var reset = resolver.GetType().GetMethod("ResetTagHelperDescriptors", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (reset == null)
                    {
                        continue;
                    }

                    reset.Invoke(resolver, Array.Empty<object>());
                }
                catch (Exception)
                {
                    // If this fails, just unsubscribe. We won't be able to do our work, so just don't waste time.
                    foreach (var workspace in _workspaces)
                    {
                        workspace.WorkspaceChanged -= Workspace_WorkspaceChanged;
                    }
                }
            }
        }
    }
}