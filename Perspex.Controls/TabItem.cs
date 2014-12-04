// -----------------------------------------------------------------------
// <copyright file="TabItem.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using Perspex.Controls.Primitives;

    public class TabItem : HeaderedContentControl, ISelectable
    {
        public static readonly PerspexProperty<bool> IsSelectedProperty =
            ListBoxItem.IsSelectedProperty.AddOwner<TabItem>();

        static TabItem()
        {
            Control.AffectsRender(IsSelectedProperty);
            Control.PseudoClass(IsSelectedProperty, ":selected");
        }

        public bool IsSelected
        {
            get { return this.GetValue(IsSelectedProperty); }
            set { this.SetValue(IsSelectedProperty, value); }
        }
    }
}
