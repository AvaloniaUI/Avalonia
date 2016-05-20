// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls.Templates;
using Avalonia.Layout;

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines a control that displays <see cref="Content"/> according to a
    /// <see cref="Avalonia.Controls.Templates.FuncDataTemplate"/>.
    /// </summary>
    public interface IContentControl : IControl
    {
        /// <summary>
        /// Gets or sets the content to display.
        /// </summary>
        object Content { get; set; }

        /// <summary>
        /// Gets or sets the data template used to display the content of the control.
        /// </summary>
        IDataTemplate ContentTemplate { get; set; }

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