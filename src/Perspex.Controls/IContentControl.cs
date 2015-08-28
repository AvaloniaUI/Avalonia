// -----------------------------------------------------------------------
// <copyright file="IContentControl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------
namespace Perspex.Controls
{
    using Perspex.Layout;

    /// <summary>
    /// Defines a control that displays <see cref="Content"/> according to a
    /// <see cref="Perspex.Controls.Templates.DataTemplate"/>.
    /// </summary>
    public interface IContentControl : IControl
    {
        /// <summary>
        /// Gets or sets the content to display.
        /// </summary>
        object Content { get; set; }

        /// <summary>
        /// Gets or sets the horizontal alignment of the content within the control.
        /// </summary>
        HorizontalAlignment HorizontalContentAlignment { get; set; }

        /// <summary>
        /// Gets or sets the vertical alignment of the content within the control.
        /// </summary>
        VerticalAlignment VerticalContentAlignment { get; set; }
    }
}