using System;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using static Avalonia.Utilities.MathUtilities;

#nullable enable

namespace Avalonia.Controls.Presenters
{
    /// <summary>
    /// Displays items inside an <see cref="ItemsControl"/>.
    /// </summary>
    public class ItemsPresenter : Control, ILogicalScrollable
    {
        private IPanel? _panel;
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

        /// <summary>
        /// Gets or sets a value indicating whether the content can be scrolled horizontally.
        /// </summary>
        bool ILogicalScrollable.CanHorizontallyScroll
        {
            get => LogicalScrollable?.CanHorizontallyScroll ?? false;
            set
            {
                if (LogicalScrollable is ILogicalScrollable logical)
                    logical.CanHorizontallyScroll = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the content can be scrolled horizontally.
        /// </summary>
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

        public override void ScrollIntoView(int index)
        {
            // TODO: Implement
        }

        protected override void PanelCreated(IPanel panel)
        {
            _scrollInvalidated?.Invoke(this, EventArgs.Empty);
            KeyboardNavigation.SetTabNavigation(
                (InputElement)Panel,
                KeyboardNavigation.GetTabNavigation(this));
        }

        private void CreatePanel()
        {
            Panel = ItemsPanel.Build();
            Panel.SetValue(TemplatedParentProperty, TemplatedParent);
            LogicalChildren.Clear();
            VisualChildren.Clear();
            LogicalChildren.Add(Panel);
            VisualChildren.Add(Panel);
            _createdPanel = true;

            if (!IsHosted && _itemsSubscription == null && Items is INotifyCollectionChanged incc)
            {
                _itemsSubscription = incc.WeakSubscribe(ItemsCollectionChanged);
            }
            
            PanelCreated(Panel);
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
