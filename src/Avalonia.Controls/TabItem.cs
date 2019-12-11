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

        /// <summary>
        /// Initializes static members of the <see cref="TabItem"/> class.
        /// </summary>
        static TabItem()
        {
            SelectableMixin.Attach<TabItem>(IsSelectedProperty);
            FocusableProperty.OverrideDefaultValue(typeof(TabItem), true);
            DataContextProperty.Changed.AddClassHandler<TabItem>((x, e) => x.UpdateHeader(e));
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

        private void UpdateHeader(AvaloniaPropertyChangedEventArgs obj)
        {
            if (Header == null)
            {
                if (obj.NewValue is IHeadered headered)
                {
                    if (Header != headered.Header)
                    {
                        Header = headered.Header;
                    }
                }
                else
                {
                    if (!(obj.NewValue is IControl))
                    {
                        Header = obj.NewValue;
                    }
                }
            }
            else
            {
                if (Header == obj.OldValue)
                {
                    Header = obj.NewValue;
                }
            }          
        }
    }
}
