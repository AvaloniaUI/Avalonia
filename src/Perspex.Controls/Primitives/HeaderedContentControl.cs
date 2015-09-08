// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Controls.Primitives
{
    /// <summary>
    /// A <see cref="ContentControl"/> with a header.
    /// </summary>
    public class HeaderedContentControl : ContentControl, IHeadered
    {
        /// <summary>
        /// Defines the <see cref="Header"/> property.
        /// </summary>
        public static readonly PerspexProperty<object> HeaderProperty =
            PerspexProperty.Register<ContentControl, object>("Header");

        /// <summary>
        /// Gets or sets the header content.
        /// </summary>
        public object Header
        {
            get { return this.GetValue(HeaderProperty); }
            set { this.SetValue(HeaderProperty, value); }
        }
    }
}