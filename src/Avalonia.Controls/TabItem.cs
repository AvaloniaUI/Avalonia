// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls.Mixins;
using Avalonia.Controls.Primitives;

namespace Avalonia.Controls
{
    /// <summary>
    /// An item in  a <see cref="TabStrip"/> or <see cref="TabControl"/>.
    /// </summary>
    public class TabItem : HeaderedContentControl, ISelectable
    {
        /// <summary>
        /// Defines the <see cref="TabStripPlacement"/> property.
        /// </summary>
        public static readonly StyledProperty<Dock> TabStripPlacementProperty =
            TabControl.TabStripPlacementProperty.AddOwner<TabItem>();   

        /// <summary>
        /// Defines the <see cref="IsSelected"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsSelectedProperty =
            ListBoxItem.IsSelectedProperty.AddOwner<TabItem>();

        private TabControl _parentTabControl;

        /// <summary>
        /// Initializes static members of the <see cref="TabItem"/> class.
        /// </summary>
        static TabItem()
        {
            SelectableMixin.Attach<TabItem>(IsSelectedProperty);
            FocusableProperty.OverrideDefaultValue(typeof(TabItem), true);
            IsSelectedProperty.Changed.AddClassHandler<TabItem>(x => x.UpdateSelectedContent);
        }

        /// <summary>
        /// Gets the tab strip placement.
        /// </summary>
        /// <value>
        /// The tab strip placement.
        /// </value>
        public Dock TabStripPlacement
        {
            get { return GetValue(TabStripPlacementProperty); }
        }

        /// <summary>
        /// Gets or sets the selection state of the item.
        /// </summary>
        public bool IsSelected
        {
            get { return GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        public TabControl ParentTabControl
        {
            get => _parentTabControl;
            set => _parentTabControl = value;
        }

        private void UpdateSelectedContent(AvaloniaPropertyChangedEventArgs e)
        {
            if (IsSelected)
            {
                ParentTabControl.SelectedContentTemplate = ContentTemplate;
                ParentTabControl.SelectedContent = Content;
            }
        }
    }
}
