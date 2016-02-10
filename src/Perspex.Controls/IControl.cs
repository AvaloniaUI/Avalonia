// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Controls.Templates;
using Perspex.Input;
using Perspex.Layout;
using Perspex.Styling;

namespace Perspex.Controls
{
    /// <summary>
    /// Interface for Perspex controls.
    /// </summary>
    public interface IControl : IVisual, ILogical, ILayoutable, IInputElement, INamed, IStyleable, IStyleHost
    {
        /// <summary>
        /// Raised when the control is attached to a rooted logical tree.
        /// </summary>
        event EventHandler<LogicalTreeAttachmentEventArgs> AttachedToLogicalTree;

        /// <summary>
        /// Raised when the control is detached from a rooted logical tree.
        /// </summary>
        event EventHandler<LogicalTreeAttachmentEventArgs> DetachedFromLogicalTree;

        /// <summary>
        /// Gets or sets the control's styling classes.
        /// </summary>
        new Classes Classes { get; set; }

        /// <summary>
        /// Gets or sets the control's data context.
        /// </summary>
        object DataContext { get; set; }

        /// <summary>
        /// Gets the data templates for the control.
        /// </summary>
        DataTemplates DataTemplates { get; }

        /// <summary>
        /// Gets the control's logical parent.
        /// </summary>
        IControl Parent { get; }
    }
}