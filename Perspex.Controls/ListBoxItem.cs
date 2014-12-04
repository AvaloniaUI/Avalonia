// -----------------------------------------------------------------------
// <copyright file="ListBoxItem.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    public class ListBoxItem : ContentControl, ISelectable
    {
        public static readonly PerspexProperty<bool> IsSelectedProperty =
            PerspexProperty.Register<ListBoxItem, bool>("IsSelected");

        static ListBoxItem()
        {
            PseudoClass(IsSelectedProperty, ":selected");
        }

        public bool IsSelected
        {
            get { return this.GetValue(IsSelectedProperty); }
            set { this.SetValue(IsSelectedProperty, value); }
        }
    }
}
