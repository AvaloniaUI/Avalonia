// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Specialized;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;
using static Avalonia.Utilities.MathUtilities;

namespace Avalonia.Controls.Presenters
{
    /// <summary>
    /// Displays items inside an <see cref="ItemsControl"/>.
    /// </summary>
    public class ItemsPresenter : ItemsPresenterBase, IScrollable
    {
        /// <summary>
        /// Defines the <see cref="VirtualizationMode"/> property.
        /// </summary>
        public static readonly StyledProperty<ItemVirtualizationMode> VirtualizationModeProperty =
            AvaloniaProperty.Register<ItemsPresenter, ItemVirtualizationMode>(
                nameof(VirtualizationMode),
                defaultValue: ItemVirtualizationMode.Simple);

        private ItemVirtualizer _virtualizer;

        /// <summary>
        /// Initializes static members of the <see cref="ItemsPresenter"/> class.
        /// </summary>
        static ItemsPresenter()
        {
            KeyboardNavigation.TabNavigationProperty.OverrideDefaultValue(
                typeof(ItemsPresenter),
                KeyboardNavigationMode.Once);
        }

        /// <summary>
        /// Gets or sets the virtualization mode for the items.
        /// </summary>
        public ItemVirtualizationMode VirtualizationMode
        {
            get { return GetValue(VirtualizationModeProperty); }
            set { SetValue(VirtualizationModeProperty, value); }
        }

        /// <inheritdoc/>
        bool IScrollable.IsLogicalScrollEnabled
        {
            get { return _virtualizer?.IsLogicalScrollEnabled ?? false; }
        }

        /// <inheritdoc/>
        Action IScrollable.InvalidateScroll { get; set; }

        /// <inheritdoc/>
        Size IScrollable.Extent => _virtualizer.Extent;

        /// <inheritdoc/>
        Vector IScrollable.Offset
        {
            get { return _virtualizer.Offset; }
            set { _virtualizer.Offset = CoerceOffset(value); }
        }

        /// <inheritdoc/>
        Size IScrollable.Viewport => _virtualizer.Viewport;

        /// <inheritdoc/>
        Size IScrollable.ScrollSize => new Size(0, 1);

        /// <inheritdoc/>
        Size IScrollable.PageScrollSize => new Size(0, 1);

        /// <inheritdoc/>
        bool IScrollable.BringIntoView(IVisual target, Rect targetRect)
        {
            return _virtualizer?.BringIntoView(target, targetRect) ?? false;
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var result = base.ArrangeOverride(finalSize);
            _virtualizer.Arranging(finalSize);
            return result;
        }

        /// <inheritdoc/>
        protected override void PanelCreated(IPanel panel)
        {
            _virtualizer = ItemVirtualizer.Create(this);
            ((IScrollable)this).InvalidateScroll?.Invoke();

            if (!Panel.IsSet(KeyboardNavigation.DirectionalNavigationProperty))
            {
                KeyboardNavigation.SetDirectionalNavigation(
                    (InputElement)Panel,
                    KeyboardNavigationMode.Contained);
            }

            KeyboardNavigation.SetTabNavigation(
                (InputElement)Panel,
                KeyboardNavigation.GetTabNavigation(this));
        }

        protected override void ItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            _virtualizer?.ItemsChanged(Items, e);
        }

        private Vector CoerceOffset(Vector value)
        {
            var scrollable = (IScrollable)this;
            var maxX = Math.Max(scrollable.Extent.Width - scrollable.Viewport.Width, 0);
            var maxY = Math.Max(scrollable.Extent.Height - scrollable.Viewport.Height, 0);
            return new Vector(Clamp(value.X, 0, maxX), Clamp(value.Y, 0, maxY));
        }
    }
}