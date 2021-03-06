﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Notification;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface ICodeActionEditHandlerService
    {
        ITextBufferAssociatedViewService AssociatedViewService { get; }
        object GetPreview(Workspace workspace, IEnumerable<CodeActionOperation> operations, CancellationToken cancellationToken, DocumentId preferredDocumentId = null, ProjectId preferredProjectId = null);
        SolutionPreviewResult GetPreviews(Workspace workspace, IEnumerable<CodeActionOperation> operations, CancellationToken cancellationToken);
        void Apply(Workspace workspace, Document fromDocument, IEnumerable<CodeActionOperation> operations, string title, CancellationToken cancellationToken);
    }
}
