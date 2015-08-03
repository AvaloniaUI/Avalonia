// -----------------------------------------------------------------------
// <copyright file="TabControl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using Perspex.Animation;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Primitives;

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
        /// Gets the <see cref="SelectedItem"/> as a <see cref="TabItem"/>.
        /// </summary>
        public TabItem SelectedTab
        {
            get { return this.GetValue(SelectedTabProperty); }
            private set { this.SetValue(SelectedTabProperty, value); }
        }

        /// <summary>
        /// Gets or sets the transition to use when switching tabs.
        /// </summary>
        public IPageTransition Transition
        {
            get { return this.GetValue(TransitionProperty); }
            set { this.SetValue(TransitionProperty, value); }
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
        /// Called when the <see cref="SelectedIndex"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void SelectedIndexChanged(PerspexPropertyChangedEventArgs e)
        {
            if ((int)e.NewValue != -1)
            {
                var item = this.SelectedItem as IContentControl;
                var content = item?.Content ?? item;
                this.SelectedTab = item as TabItem;
            }
        }
    }
}
