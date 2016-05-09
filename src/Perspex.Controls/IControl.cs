// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Controls.Templates;
using Perspex.Input;
using Perspex.Layout;
using Perspex.LogicalTree;
using Perspex.Styling;
using Perspex.VisualTree;

namespace Perspex.Controls
{
    /// <summary>
    /// Interface for Perspex controls.
    /// </summary>
    public interface IControl : IVisual, ILogical, ILayoutable, IInputElement, INamed, IStyleable, IStyleHost
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
        /// Gets the data templates for the control.
        /// </summary>
        DataTemplates DataTemplates { get; }

        /// <summary>
        /// Gets the <see cref="IDataTemplate"/> that this control was materialized from.
        /// </summary>
        IDataTemplate MaterializedFrom { get; }

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