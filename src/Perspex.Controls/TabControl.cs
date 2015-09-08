// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Animation;
using Perspex.Controls.Presenters;
using Perspex.Controls.Primitives;

namespace Perspex.Controls
{
    /// <summary>
    /// A tab control that displays a tab strip along with the content of the selected tab.
    /// </summary>
    public class TabControl : SelectingItemsControl, IReparentingHost
    {
        /// <summary>
        /// Defines the <see cref="SelectedTab"/> property.
        /// </summary>
        public static readonly PerspexProperty<TabItem> SelectedTabProperty =
            PerspexProperty.Register<TabControl, TabItem>("SelectedTab");

        /// <summary>
        /// Defines the <see cref="Transition"/> property.
        /// </summary>
        public static readonly PerspexProperty<IPageTransition> TransitionProperty =
            Deck.TransitionProperty.AddOwner<TabControl>();

        /// <summary>
        /// Initializes static members of the <see cref="TabControl"/> class.
        /// </summary>
        static TabControl()
        {
            AutoSelectProperty.OverrideDefaultValue<TabControl>(true);
            FocusableProperty.OverrideDefaultValue<TabControl>(false);
            SelectedIndexProperty.Changed.AddClassHandler<TabControl>(x => x.SelectedIndexChanged);
        }

        /// <summary>
        /// Gets the <see cref="SelectingItemsControl.SelectedItem"/> as a <see cref="TabItem"/>.
        /// </summary>
        public TabItem SelectedTab
        {
            get { return GetValue(SelectedTabProperty); }
            private set { SetValue(SelectedTabProperty, value); }
        }

        /// <summary>
        /// Gets or sets the transition to use when switching tabs.
        /// </summary>
        public IPageTransition Transition
        {
            get { return GetValue(TransitionProperty); }
            set { SetValue(TransitionProperty, value); }
        }

        /// <summary>
        /// Asks the control whether it wants to reparent the logical children of the specified
        /// control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>
        /// True if the control wants to reparent its logical children otherwise false.
        /// </returns>
        bool IReparentingHost.WillReparentChildrenOf(IControl control)
        {
            return control is DeckPresenter;
        }

        /// <summary>
        /// Called when the <see cref="SelectingItemsControl.SelectedIndex"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void SelectedIndexChanged(PerspexPropertyChangedEventArgs e)
        {
            if ((int)e.NewValue != -1)
            {
                var item = SelectedItem as IContentControl;
                var content = item?.Content ?? item;
                SelectedTab = item as TabItem;
            }
        }
    }
}
