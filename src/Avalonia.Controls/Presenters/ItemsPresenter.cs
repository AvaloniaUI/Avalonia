using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Presenters
{
    /// <summary>
    /// Presents items inside an <see cref="Avalonia.Controls.ItemsControl"/>.
    /// </summary>
    public class ItemsPresenter : Control, ILogicalScrollable
    {
        /// <summary>
        /// Defines the <see cref="ItemsPanel"/> property.
        /// </summary>
        public static readonly StyledProperty<ITemplate<Panel?>> ItemsPanelProperty =
            ItemsControl.ItemsPanelProperty.AddOwner<ItemsPresenter>();

        private PanelContainerGenerator? _generator;
        private ILogicalScrollable? _logicalScrollable;
        private EventHandler? _scrollInvalidated;
        private ScrollIntoViewRequest? _scrollIntoViewRequest;
        private int _pendingScrollIntoViewIndex = -1;

        event EventHandler? ILogicalScrollable.ScrollInvalidated
        {
            add => _scrollInvalidated += value;
            remove => _scrollInvalidated -= value;
        }

        /// <summary>
        /// Gets or sets a template which creates the <see cref="Panel"/> used to display the items.
        /// </summary>
        public ITemplate<Panel?> ItemsPanel
        {
            get => GetValue(ItemsPanelProperty);
            set => SetValue(ItemsPanelProperty, value);
        }

        /// <summary>
        /// Gets the panel used to display the items.
        /// </summary>
        public Panel? Panel { get; private set; }

        /// <summary>
        /// Gets the owner <see cref="ItemsControl"/>.
        /// </summary>
        internal ItemsControl? ItemsControl { get; private set; }

        private bool CanHorizontallyScroll
            => _logicalScrollable?.CanHorizontallyScroll ?? false;
        
        bool ILogicalScrollable.CanHorizontallyScroll
        {
            get => CanHorizontallyScroll;
            set => _logicalScrollable?.CanHorizontallyScroll = value;
        }

        bool IScrollable.CanHorizontallyScroll
            => CanHorizontallyScroll;

        private bool CanVerticallyScroll
            => _logicalScrollable?.CanVerticallyScroll ?? false;

        bool ILogicalScrollable.CanVerticallyScroll
        {
            get => CanVerticallyScroll;
            set => _logicalScrollable?.CanVerticallyScroll = value;
        }

        bool IScrollable.CanVerticallyScroll
            => CanVerticallyScroll;

        Vector IScrollable.Offset 
        {
            get => _logicalScrollable?.Offset ?? default;
            set => _logicalScrollable?.Offset = value;
        }

        bool ILogicalScrollable.IsLogicalScrollEnabled => _logicalScrollable?.IsLogicalScrollEnabled ?? false;
        Size ILogicalScrollable.ScrollSize => _logicalScrollable?.ScrollSize ?? default;
        Size ILogicalScrollable.PageScrollSize => _logicalScrollable?.PageScrollSize ?? default;
        Size IScrollable.Extent => _logicalScrollable?.Extent ?? default;
        Size IScrollable.Viewport => _logicalScrollable?.Viewport ?? default;


        public override sealed void ApplyTemplate()
        {
            if (Panel is null && ItemsControl is not null)
            {
                if (_logicalScrollable is not null)
                {
                    _logicalScrollable.ScrollInvalidated -= OnLogicalScrollInvalidated;
                }

                Panel = ItemsPanel.Build();

                if (Panel is null)
                {
                    return;
                }

                Panel.TemplatedParent = TemplatedParent;
                Panel.IsItemsHost = true;
                LogicalChildren.Add(Panel);
                VisualChildren.Add(Panel);

                if (Panel is VirtualizingPanel v)
                    v.Attach(ItemsControl);
                else
                    CreateSimplePanelGenerator();

                _logicalScrollable = Panel as ILogicalScrollable;

                if (_logicalScrollable is not null)
                {
                    _logicalScrollable.ScrollInvalidated += OnLogicalScrollInvalidated;
                }
            }
        }

        bool ILogicalScrollable.BringIntoView(Control target, Rect targetRect) =>
            _logicalScrollable?.BringIntoView(target, targetRect) ?? false;
        Control? ILogicalScrollable.GetControlInDirection(NavigationDirection direction, Control? from) =>
            _logicalScrollable?.GetControlInDirection(direction, from);
        void ILogicalScrollable.RaiseScrollInvalidated(EventArgs e) => _scrollInvalidated?.Invoke(this, e);

        internal void ScrollIntoView(int index)
        {
            if (index < 0)
                return;

            // Not attached to a layout root yet: the request will be enqueued in OnAttachedToVisualTree.
            if (this.GetLayoutRoot()?.LayoutManager is not IBringIntoViewLayoutManager layoutManager)
            {
                _pendingScrollIntoViewIndex = index;
                return;
            }

            // Try to execute synchronously when no layout pass is running.
            if (!layoutManager.IsInLayoutPass && TryScrollIntoViewNow(index))
            {
                _pendingScrollIntoViewIndex = -1;
                return;
            }

            // Defer to the end of the layout pass.
            _pendingScrollIntoViewIndex = index;
            layoutManager.EnqueueBringIntoView(_scrollIntoViewRequest ??= new(this));
        }

        internal void CancelScrollIntoView(int index)
        {
            if (_pendingScrollIntoViewIndex == index)
                _pendingScrollIntoViewIndex = -1;
        }

        private bool TryScrollIntoViewNow(int index)
        {
            if (!IsEffectivelyVisible || Panel is not { } panel)
                return false;

            if (panel is VirtualizingPanel virtualizingPanel)
                return virtualizingPanel.ScrollIntoView(index) is not null;

            if (index < panel.Children.Count)
            {
                panel.Children[index].BringIntoView();
                return true;
            }

            return false;
        }

        private bool TryExecutePendingScrollIntoView()
        {
            var index = _pendingScrollIntoViewIndex;
            if (index < 0)
                return true;

            if (Panel is not { IsMeasureValid: true, IsArrangeValid: true } || !IsEffectivelyVisible)
                return false;

            _pendingScrollIntoViewIndex = -1;

            // Try to scroll, but ignore the return value. We don't want to return false if this fails:
            // this would requeue a request that might never been be fulfilled because items changed.
            _ = TryScrollIntoViewNow(index);

            // Executing the scroll may have prepared containers whose handlers requested a new
            // scroll: in that case keep the request queued so it gets executed as well.
            return _pendingScrollIntoViewIndex < 0;
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (_pendingScrollIntoViewIndex >= 0 && this.GetLayoutRoot()?.LayoutManager is IBringIntoViewLayoutManager layoutManager)
                layoutManager.EnqueueBringIntoView(_scrollIntoViewRequest ??= new(this));
        }

        private sealed class ScrollIntoViewRequest(ItemsPresenter presenter)
            : BringIntoViewRequest(presenter)
        {
            public override bool TryExecute()
                => presenter.TryExecutePendingScrollIntoView();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == TemplatedParentProperty)
            {
                ResetState();
                ItemsControl = null;

                if (change.NewValue is ItemsControl itemsControl)
                {
                    ItemsControl = itemsControl;
                    ItemsControl.RegisterItemsPresenter(this);
                }
            }
            else if (change.Property == ItemsPanelProperty)
            {
                ResetState();
                InvalidateMeasure();
            }
        }

        internal void Refresh()
        {
            if (Panel is VirtualizingPanel v)
                v.Refresh();
            else
                _generator?.Refresh();
        }

        private void ResetState()
        {
            _generator?.Dispose();
            _generator = null;
            LogicalChildren.Clear();
            VisualChildren.Clear();
            (Panel as VirtualizingPanel)?.Detach();
            Panel = null;
        }

        private void CreateSimplePanelGenerator()
        {
            Debug.Assert(Panel is not VirtualizingPanel);

            if (ItemsControl is null || Panel is null)
                return;

            _generator?.Dispose();
            _generator = new(this);
        }

        internal Control? ContainerFromIndex(int index)
        {
            if (Panel is VirtualizingPanel v)
                return v.ContainerFromIndex(index);
            return index >= 0 && index < Panel?.Children.Count ? Panel.Children[index] : null;
        }

        internal IEnumerable<Control>? GetRealizedContainers()
        {
            if (Panel is VirtualizingPanel v)
                return v.GetRealizedContainers();
            return Panel?.Children;
        }
        
        internal int IndexFromContainer(Control container)
        {
            if (Panel is VirtualizingPanel v)
                return v.IndexFromContainer(container);
            return Panel?.Children.IndexOf(container) ?? -1;
        }

        private void OnLogicalScrollInvalidated(object? sender, EventArgs e) => _scrollInvalidated?.Invoke(this, e);
    }
}
