// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// Interface for Avalonia controls.
    /// </summary>
    public interface IControl : IVisual,
        IDataTemplateHost,
        ILogical,
        ILayoutable,
        IInputElement,
        INamed,
        IResourceNode,
        IStyleable,
        IStyleHost
    {
        /// <summary>
        /// Occurs when the control has finished initialization.
        /// </summary>
        event EventHandler Initialized;

        /// <summary>
        /// Gets or sets the control's styling classes.
        /// </summary>
        new Classes Classes { get; set; }

        /// <summary>
        /// Gets or sets the control's data context.
        /// </summary>
        object DataContext { get; set; }

        /// <summary>
        /// Gets a value that indicates whether the element has finished initialization.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Gets the control's logical parent.
        /// </summary>
        IControl Parent { get; }
    }
}