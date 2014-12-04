// -----------------------------------------------------------------------
// <copyright file="HeaderedContentControl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Primitives
{
    public class HeaderedContentControl : ContentControl, IHeadered
    {
        public static readonly PerspexProperty<object> HeaderProperty =
            PerspexProperty.Register<ContentControl, object>("Header");

        public object Header
        {
            get { return this.GetValue(HeaderProperty); }
            set { this.SetValue(HeaderProperty, value); }
        }
    }
}
