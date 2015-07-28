// -----------------------------------------------------------------------
// <copyright file="Deck.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System.Collections;
    using Perspex.Animation;
    using Perspex.Controls.Generators;
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Utils;
    using Perspex.Input;

    /// <summary>
    /// A selecting items control that displays a single item that fills the control.
    /// </summary>
    public class Deck : SelectingItemsControl
    {
        public static readonly PerspexProperty<IPageTransition> TransitionProperty =
            PerspexProperty.Register<Deck, IPageTransition>("Transition");

        private static readonly ItemsPanelTemplate PanelTemplate = 
            new ItemsPanelTemplate(() => new Panel());

        static Deck()
        {
            ItemsPanelProperty.OverrideDefaultValue(typeof(Deck), PanelTemplate);
        }

        public IPageTransition Transition
        {
            get { return this.GetValue(TransitionProperty); }
            set { this.SetValue(TransitionProperty, value); }
        }

        protected override void ItemsChanged(PerspexPropertyChangedEventArgs e)
        {
            base.ItemsChanged(e);

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

        protected override void OnTemplateApplied()
        {
            base.OnTemplateApplied();
        }
    }
}
