// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Layout;

namespace Perspex.Controls
{
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