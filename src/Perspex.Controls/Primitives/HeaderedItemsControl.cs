// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Controls.Primitives
{
    public class HeaderedItemsControl : ItemsControl
    {
        public static readonly PerspexProperty<object> HeaderProperty =
            HeaderedContentControl.HeaderProperty.AddOwner<HeaderedItemsControl>();

        public object Header
        {
            get { return this.GetValue(HeaderProperty); }
            set { this.SetValue(HeaderProperty, value); }
        }
    }
}
