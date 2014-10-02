// -----------------------------------------------------------------------
// <copyright file="SelectingItemsControl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Primitives
{
    public class SelectingItemsControl : ItemsControl
    {
        public static readonly PerspexProperty<object> SelectedItemProperty =
            PerspexProperty.Register<SelectingItemsControl, object>("SelectedItem");

        public object SelectedItem
        {
            get { return this.GetValue(SelectedItemProperty); }
            set { this.SetValue(SelectedItemProperty, value); }
        }
    }
}
