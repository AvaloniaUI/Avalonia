// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// A <see cref="ContentControl"/> with a header.
    /// </summary>
    public class HeaderedContentControl : ContentControl, IHeadered
    {
        /// <summary>
        /// Defines the <see cref="Header"/> property.
        /// </summary>
        public static readonly StyledProperty<object> HeaderProperty =
            AvaloniaProperty.Register<ContentControl, object>(nameof(Header));

        /// <summary>
        /// Gets or sets the header content.
        /// </summary>
        public object Header
        {
            get { return GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }
    }
}