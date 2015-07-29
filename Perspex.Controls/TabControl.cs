// -----------------------------------------------------------------------
// <copyright file="TabControl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using Perspex.Animation;
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Templates;

    /// <summary>
    /// A tab control that displays a tab strip along with the content of the selected tab.
    /// </summary>
    public class TabControl : SelectingItemsControl
    {
        /// <summary>
        /// Defines the <see cref="SelectedContent"/> property.
        /// </summary>
        public static readonly PerspexProperty<object> SelectedContentProperty =
            PerspexProperty.Register<TabControl, object>("SelectedContent");

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
        /// Gets the content of the selected tab.
        /// </summary>
        public object SelectedContent
        {
            get { return this.GetValue(SelectedContentProperty); }
            private set { this.SetValue(SelectedContentProperty, value); }
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

        /// <inheritdoc/>
        protected override void OnTemplateApplied()
        {
            base.OnTemplateApplied();

            var deck = this.GetTemplateChild<Deck>("deck");
            ((IReparentingControl)deck.Presenter.Panel).ReparentLogicalChildren(
                this,
                this.LogicalChildren);
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
                this.SelectedContent = content;
            }
        }
    }
}
