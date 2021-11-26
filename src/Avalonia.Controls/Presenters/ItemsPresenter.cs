using System;
using System.Collections.Generic;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using static Avalonia.Utilities.MathUtilities;

#nullable enable

namespace Avalonia.Controls.Presenters
{
    /// <summary>
    /// Displays items inside an <see cref="ItemsControl"/>.
    /// </summary>
    public class ItemsPresenter : Control, IItemsPresenter, ILogicalScrollable
    {
        private bool _createdPanel;
        private EventHandler? _scrollInvalidated;

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
        /// Gets the panel used to display the items.
        /// </summary>
        public IPanel? Panel { get; private set; }

        public IEnumerable<IControl> RealizedElements
        {
            get
            {
                if (Panel is IVirtualizingPanel vp)
                    return vp.RealizedElements;
                else if (Panel is not null)
                    return Panel.Children;
                else
                    return Array.Empty<IControl>();
            }
        }

        IItemContainerGenerator? IItemsPresenter.ItemContainerGenerator => ItemsControl?.ItemContainerGenerator;
        ItemsSourceView? IItemsPresenter.ItemsView => ItemsControl?.ItemsView;

        bool ILogicalScrollable.CanHorizontallyScroll
        {
            get => LogicalScrollable?.CanHorizontallyScroll ?? false;
            set
            {
                if (LogicalScrollable is ILogicalScrollable logical)
                    logical.CanHorizontallyScroll = value;
            }
        }

        bool ILogicalScrollable.CanVerticallyScroll
        {
            get => LogicalScrollable?.CanVerticallyScroll ?? false;
            set
            {
                if (LogicalScrollable is ILogicalScrollable logical)
                    logical.CanVerticallyScroll = value;
            }
        }

        bool ILogicalScrollable.IsLogicalScrollEnabled => LogicalScrollable is not null;
        Size ILogicalScrollable.ScrollSize => LogicalScrollable?.ScrollSize ?? new Size(ScrollViewer.DefaultSmallChange, 1);
        Size ILogicalScrollable.PageScrollSize => LogicalScrollable?.Viewport ?? new Size(16, 16);
        Size IScrollable.Extent => LogicalScrollable?.Extent ?? Size.Empty;

        Vector IScrollable.Offset
        {
            get => LogicalScrollable?.Offset ?? default;
            set
            {
                if (LogicalScrollable is ILogicalScrollable logical)
                    logical.Offset = CoerceOffset(value);
            }
        }

        Size IScrollable.Viewport => LogicalScrollable?.Viewport ?? Bounds.Size;

        private ItemsControl? ItemsControl => TemplatedParent as ItemsControl;

        private ILogicalScrollable? LogicalScrollable
        {
            get
            {
                return Panel is ILogicalScrollable logical && logical.IsLogicalScrollEnabled ?
                    logical : null;
            }
        }

        event EventHandler? ILogicalScrollable.ScrollInvalidated
        {
            add => _scrollInvalidated += value;
            remove => _scrollInvalidated -= value;
        }

        public override void ApplyTemplate()
        {
            if (!_createdPanel)
                CreatePanel();
        }

        bool ILogicalScrollable.BringIntoView(IControl target, Rect targetRect)
        {
            // TODO: Implement
            return false;
        }

        IControl? ILogicalScrollable.GetControlInDirection(NavigationDirection direction, IControl from)
        {
            return LogicalScrollable?.GetControlInDirection(direction, from);
        }

        void ILogicalScrollable.RaiseScrollInvalidated(EventArgs e)
        {
            _scrollInvalidated?.Invoke(this, e);
        }

        private void CreatePanel()
        {
            if (TemplatedParent is ItemsControl itemsControl)
            {
                Panel = itemsControl.ItemsPanel.Build();
                Panel.SetValue(TemplatedParentProperty, TemplatedParent);
                LogicalChildren.Clear();
                VisualChildren.Clear();
                LogicalChildren.Add(Panel);
                VisualChildren.Add(Panel);
                _createdPanel = true;

                KeyboardNavigation.SetTabNavigation(
                    (InputElement)Panel,
                    KeyboardNavigation.GetTabNavigation(this));
            }
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
            _scrollInvalidated?.Invoke(this, EventArgs.Empty);
        }
    }
}
