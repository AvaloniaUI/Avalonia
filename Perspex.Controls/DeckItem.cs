// -----------------------------------------------------------------------
// <copyright file="DeckItem.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    public class DeckItem : ContentControl, ISelectable
    {
        public static readonly PerspexProperty<bool> IsSelectedProperty =
            PerspexProperty.Register<DeckItem, bool>("IsSelected");

        static DeckItem()
        {
            Control.PseudoClass(IsSelectedProperty, ":selected");
        }

        public bool IsSelected
        {
            get { return this.GetValue(IsSelectedProperty); }
            set { this.SetValue(IsSelectedProperty, value); }
        }
    }
}
