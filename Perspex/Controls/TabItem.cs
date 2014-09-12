// -----------------------------------------------------------------------
// <copyright file="TabItem.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    public class TabItem : HeaderedContentControl
    {
        public static readonly PerspexProperty<bool> IsSelectedProperty =
            PerspexProperty.Register<TabItem, bool>("IsSelected");

        public TabItem()
        {
            this.AddPseudoClass(IsSelectedProperty, ":selected");
            AffectsRender(IsSelectedProperty);
        }

        public bool IsSelected
        {
            get { return this.GetValue(IsSelectedProperty); }
            set { this.SetValue(IsSelectedProperty, value); }
        }
    }
}
