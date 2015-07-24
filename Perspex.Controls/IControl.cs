// -----------------------------------------------------------------------
// <copyright file="IControl.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using Perspex.Input;
    using Perspex.Layout;
    using Perspex.Styling;

    /// <summary>
    /// Interface for Perspex controls.
    /// </summary>
    public interface IControl : IVisual, ILogical, ILayoutable, IInputElement, INamed, IStyleable, IStyleHost
    {
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