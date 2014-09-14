// -----------------------------------------------------------------------
// <copyright file="HeaderedItemsControl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Collections;
    using System.Linq;
    using System.Reactive.Linq;

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
