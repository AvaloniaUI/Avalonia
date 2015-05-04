// -----------------------------------------------------------------------
// <copyright file="Deck.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System.Collections;
    using Perspex.Controls.Generators;
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Utils;
    using Perspex.Input;

    /// <summary>
    /// A selecting items control that displays a single item that fills the control.
    /// </summary>
    public class Deck : SelectingItemsControl
    {
        private static readonly ItemsPanelTemplate PanelTemplate = 
            new ItemsPanelTemplate(() => new Panel());

        static Deck()
        {
            ItemsPanelProperty.OverrideDefaultValue(typeof(Deck), PanelTemplate);
        }

        protected override ItemContainerGenerator CreateItemContainerGenerator()
        {
            return new TypedItemContainerGenerator<DeckItem>(this);
        }

        protected override void ItemsChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            base.ItemsChanged(oldValue, newValue);

            var items = this.Items;

            if (items != null && items.Count() > 0)
            {
                this.SelectedIndex = 0;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            // Ignore key presses.
        }

        protected override void OnPointerPressed(PointerPressEventArgs e)
        {
            // Ignore pointer presses.
        }
    }
}
