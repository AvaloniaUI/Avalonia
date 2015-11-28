// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Animation;
using Perspex.Controls.Presenters;
using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;

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
            Carousel.TransitionProperty.AddOwner<TabControl>();

        private static readonly IMemberSelector s_contentSelector =
            new FuncMemberSelector<object, object>(SelectContent);

        /// <summary>
        /// Defines the <see cref="TabStripPlacement"/> property.
        /// </summary>
        public static readonly PerspexProperty<Dock> TabStripPlacementProperty =
            PerspexProperty.Register<TabControl, Dock>(nameof(TabStripPlacement), defaultValue: Dock.Top);

        /// <summary>
        /// Initializes static members of the <see cref="TabControl"/> class.
        /// </summary>
        static TabControl()
        {
            SelectionModeProperty.OverrideDefaultValue<TabControl>(SelectionMode.AlwaysSelected);
            FocusableProperty.OverrideDefaultValue<TabControl>(false);
            SelectedItemProperty.Changed.AddClassHandler<TabControl>(x => x.SelectedItemChanged);
            AffectsMeasure(TabStripPlacementProperty);
        }

        /// <summary>
        /// Gets an <see cref="IMemberSelector"/> that selects the content of a <see cref="TabItem"/>.
        /// </summary>
        public IMemberSelector ContentSelector => s_contentSelector;

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
        /// Gets or sets the tabstrip placement of the tabcontrol.
        /// </summary>
        public Dock TabStripPlacement
        {
            get { return GetValue(TabStripPlacementProperty); }
            set { SetValue(TabStripPlacementProperty, value); }
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
            return control is CarouselPresenter;
        }

        /// <summary>
        /// Selects the content of a tab item.
        /// </summary>
        /// <param name="o">The tab item.</param>
        /// <returns>The content.</returns>
        private static object SelectContent(object o)
        {
            var content = o as IContentControl;

            if (content != null)
            {
                return content.Content;
            }
            else
            {
                return o;
            }       
        }

        /// <summary>
        /// Called when the <see cref="SelectingItemsControl.SelectedIndex"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void SelectedItemChanged(PerspexPropertyChangedEventArgs e)
        {
            var item = e.NewValue as IContentControl;
            var content = item?.Content ?? item;
            SelectedTab = item as TabItem;
        }
    }
}
