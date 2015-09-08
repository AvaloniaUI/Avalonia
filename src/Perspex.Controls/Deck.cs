// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Animation;
using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;
using Perspex.Controls.Utils;
using Perspex.Input;

namespace Perspex.Controls
{
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
        private static readonly ITemplate<IPanel> s_panelTemplate =
            new FuncTemplate<IPanel>(() => new Panel());

        /// <summary>
        /// Initializes static members of the <see cref="Deck"/> class.
        /// </summary>
        static Deck()
        {
            AutoSelectProperty.OverrideDefaultValue<Deck>(true);
            ItemsPanelProperty.OverrideDefaultValue<Deck>(s_panelTemplate);
        }

        /// <summary>
        /// Gets or sets the transition to use when moving between pages.
        /// </summary>
        public IPageTransition Transition
        {
            get { return GetValue(TransitionProperty); }
            set { SetValue(TransitionProperty, value); }
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
