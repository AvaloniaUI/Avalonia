// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Specialized;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using static Avalonia.Utilities.MathUtilities;

namespace Avalonia.Controls.Presenters
{
    /// <summary>
    /// Displays items inside an <see cref="ItemsControl"/>.
    /// </summary>
    public class ItemsPresenter : ItemsPresenterBase, ILogicalScrollable
    {
        /// <summary>
        /// Defines the <see cref="VirtualizationMode"/> property.
        /// </summary>
        public static readonly StyledProperty<ItemVirtualizationMode> VirtualizationModeProperty =
            AvaloniaProperty.Register<ItemsPresenter, ItemVirtualizationMode>(
                nameof(VirtualizationMode),
                defaultValue: ItemVirtualizationMode.None);

        private ItemVirtualizer _virtualizer;
        private bool _canHorizontallyScroll;
        private bool _canVerticallyScroll;

        /// <summary>
        /// Initializes static members of the <see cref="ItemsPresenter"/> class.
        /// </summary>
        static ItemsPresenter()
        {
            KeyboardNavigation.TabNavigationProperty.OverrideDefaultValue(
                typeof(ItemsPresenter),
                KeyboardNavigationMode.Once);

            VirtualizationModeProperty.Changed
                .AddClassHandler<ItemsPresenter>(x => x.VirtualizationModeChanged);
        }

        /// <summary>
        /// Gets or sets the virtualization mode for the items.
        /// </summary>
        public ItemVirtualizationMode VirtualizationMode
        {
            get { return GetValue(VirtualizationModeProperty); }
            set { SetValue(VirtualizationModeProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the content can be scrolled horizontally.
        /// </summary>
        bool ILogicalScrollable.CanHorizontallyScroll
        {
            get { return _canHorizontallyScroll; }
            set
            {
                _canHorizontallyScroll = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the content can be scrolled horizontally.
        /// </summary>
        bool ILogicalScrollable.CanVerticallyScroll
        {
            get { return _canVerticallyScroll; }
            set
            {
                _canVerticallyScroll = value;
                InvalidateMeasure();
            }
        }
        /// <inheritdoc/>
        bool ILogicalScrollable.IsLogicalScrollEnabled
        {
            get { return _virtualizer?.IsLogicalScrollEnabled ?? false; }
        }

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
        Action ILogicalScrollable.InvalidateScroll { get; set; }

        /// <inheritdoc/>
        Size ILogicalScrollable.ScrollSize => new Size(1, 1);

        /// <inheritdoc/>
        Size ILogicalScrollable.PageScrollSize => new Size(0, 1);

        /// <inheritdoc/>
        bool ILogicalScrollable.BringIntoView(IControl target, Rect targetRect)
        {
            return false;
        }

        /// <inheritdoc/>
        IControl ILogicalScrollable.GetControlInDirection(NavigationDirection direction, IControl from)
        {
            return _virtualizer?.GetControlInDirection(direction, from);
        }

        public override void ScrollIntoView(object item)
        {
            _virtualizer?.ScrollIntoView(item);
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            return _virtualizer?.MeasureOverride(availableSize) ?? Size.Empty;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            return _virtualizer?.ArrangeOverride(finalSize) ?? Size.Empty;
        }

        /// <inheritdoc/>
        protected override void PanelCreated(IPanel panel)
        {
            _virtualizer = ItemVirtualizer.Create(this);
            ((ILogicalScrollable)this).InvalidateScroll?.Invoke();

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
            var scrollable = (ILogicalScrollable)this;
            var maxX = Math.Max(scrollable.Extent.Width - scrollable.Viewport.Width, 0);
            var maxY = Math.Max(scrollable.Extent.Height - scrollable.Viewport.Height, 0);
            return new Vector(Clamp(value.X, 0, maxX), Clamp(value.Y, 0, maxY));
        }

        private void VirtualizationModeChanged(AvaloniaPropertyChangedEventArgs e)
        {
            _virtualizer?.Dispose();
            _virtualizer = ItemVirtualizer.Create(this);
            ((ILogicalScrollable)this).InvalidateScroll?.Invoke();
        }
    }
}