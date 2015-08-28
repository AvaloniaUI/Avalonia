// -----------------------------------------------------------------------
// <copyright file="Deck.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using Perspex.Animation;
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Templates;
    using Perspex.Controls.Utils;
    using Perspex.Input;

    /// <summary>
    /// An items control that displays its items as pages that fill the control.
    /// </summary>
    public class Deck : SelectingItemsControl
    {
        /// <summary>
        /// Defines the <see cref="Transition"/> property.
        /// </summary>
        public static readonly PerspexProperty<IPageTransition> TransitionProperty =
            PerspexProperty.Register<Deck, IPageTransition>("Transition");

        /// <summary>
        /// The default value of <see cref="IReparentingControl"/> for <see cref="Deck"/>.
        /// </summary>
        private static readonly ITemplate<IPanel> PanelTemplate =
            new FuncTemplate<IPanel>(() => new Panel());

        /// <summary>
        /// Initializes static members of the <see cref="Deck"/> class.
        /// </summary>
        static Deck()
        {
            AutoSelectProperty.OverrideDefaultValue<Deck>(true);
            ItemsPanelProperty.OverrideDefaultValue<Deck>(PanelTemplate);
        }

        /// <summary>
        /// Gets or sets the transition to use when moving between pages.
        /// </summary>
        public IPageTransition Transition
        {
            get { return this.GetValue(TransitionProperty); }
            set { this.SetValue(TransitionProperty, value); }
        }

        /// <inheritdoc/>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            // Ignore key presses.
        }

        /// <inheritdoc/>
        protected override void OnPointerPressed(PointerPressEventArgs e)
        {
            // Ignore pointer presses.
        }

        /// <inheritdoc/>
        protected override void OnTemplateApplied()
        {
            base.OnTemplateApplied();
        }
    }
}
