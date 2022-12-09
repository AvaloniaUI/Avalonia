using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace Avalonia.Controls
{
    /// <summary>
    /// A panel used by <see cref="Carousel"/> to display the current item.
    /// </summary>
    public class VirtualizingCarouselPanel : VirtualizingPanel, ILogicalScrollable
    {
        private static readonly AttachedProperty<bool> ItemIsOwnContainerProperty =
            AvaloniaProperty.RegisterAttached<VirtualizingCarouselPanel, Control, bool>("ItemIsOwnContainer");

        private Size _extent;
        private Vector _offset;
        private Size _viewport;
        private Stack<Control>? _recyclePool;
        private Control? _realized;
        private int _realizedIndex = -1;
        private Control? _transitionFrom;
        private int _transitionFromIndex = -1;
        private CancellationTokenSource? _transition;
        private EventHandler? _scrollInvalidated;

        bool ILogicalScrollable.CanHorizontallyScroll { get; set; }
        bool ILogicalScrollable.CanVerticallyScroll { get; set; }
        bool ILogicalScrollable.IsLogicalScrollEnabled => true;
        Size ILogicalScrollable.ScrollSize => new(1, 1);
        Size ILogicalScrollable.PageScrollSize => new(1, 1);
        Size IScrollable.Extent => Extent;
        Size IScrollable.Viewport => Viewport;

        Vector IScrollable.Offset 
        {
            get => _offset;
            set
            {
                if ((int)_offset.X != value.X)
                    InvalidateMeasure();
                _offset = value;
            }
        }

        private Size Extent
        {
            get => _extent;
            set
            {
                if (_extent != value)
                {
                    _extent = value;
                    _scrollInvalidated?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private Size Viewport
        {
            get => _viewport;
            set
            {
                if (_viewport != value)
                {
                    _viewport = value;
                    _scrollInvalidated?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        event EventHandler? ILogicalScrollable.ScrollInvalidated
        {
            add => _scrollInvalidated += value;
            remove => _scrollInvalidated -= value;
        }

        bool ILogicalScrollable.BringIntoView(Control target, Rect targetRect)
        {
            throw new NotImplementedException();
        }

        Control? ILogicalScrollable.GetControlInDirection(NavigationDirection direction, Control? from)
        {
            throw new NotImplementedException();
        }

        void ILogicalScrollable.RaiseScrollInvalidated(EventArgs e) => _scrollInvalidated?.Invoke(this, e);

        protected override Size MeasureOverride(Size availableSize)
        {
            var items = ItemsControl?.ItemsView ?? ItemsSourceView.Empty;
            var index = (int)_offset.X;

            if (index != _realizedIndex)
            {
                if (_realized is not null)
                {
                    var cancelTransition = _transition is not null;

                    // Cancel any already running transition, and recycle the element we're transitioning from.
                    if (cancelTransition)
                    {
                        _transition!.Cancel();
                        _transition = null;
                        if (_transitionFrom is not null)
                            RecycleElement(_transitionFrom);
                        _transitionFrom = null;
                        _transitionFromIndex = -1;
                    }

                    if (cancelTransition || GetTransition() is null)
                    {
                        // If don't have a transition or we've just canceled a transition then recycle the element
                        // we're moving from.
                        RecycleElement(_realized);
                    }
                    else
                    {
                        // We have a transition to do: record the current element as the element we're transitioning
                        // from and we'll start the transition in the arrange pass.
                        _transitionFrom = _realized;
                        _transitionFromIndex = _realizedIndex;
                    }

                    _realized = null;
                    _realizedIndex = -1;
                }
                
                // Get or create an element for the new item.
                if (index >= 0 && index < items.Count)
                {
                    _realized = GetOrCreateElement(items, index);
                    _realizedIndex = index;
                }
            }

            if (_realized is null)
            {
                Extent = Viewport = new(0, 0);
                _transitionFrom = null;
                _transitionFromIndex = -1;
                return default;
            }

            _realized.Measure(availableSize);
            Extent = new(items.Count, 1);
            Viewport = new(1, 1);

            return _realized.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var result = base.ArrangeOverride(finalSize);

            if (_transition is null &&
                _transitionFrom is not null &&
                _realized is { } to &&
                GetTransition() is { } transition)
            {
                _transition = new CancellationTokenSource();
                transition.Start(_transitionFrom, to, _realizedIndex > _transitionFromIndex, _transition.Token)
                    .ContinueWith(TransitionFinished, TaskScheduler.FromCurrentSynchronizationContext());
            }

            return result;
        }

        protected override IInputElement? GetControl(NavigationDirection direction, IInputElement? from, bool wrap) => null;

        protected internal override Control? ContainerFromIndex(int index)
        {
            return index == _realizedIndex ? _realized : null;
        }

        protected internal override IEnumerable<Control>? GetRealizedContainers()
        {
            return _realized is not null ? new[] { _realized } : null;
        }

        protected internal override int IndexFromContainer(Control container)
        {
            return container == _realized ? _realizedIndex : -1;
        }

        protected internal override Control? ScrollIntoView(int index)
        {
            return null;
        }

        protected override void OnItemsChanged(IReadOnlyList<object?> items, NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(items, e);

            void Add(int index, int count)
            {
                if (index <= _realizedIndex)
                    _realizedIndex += count;
            }

            void Remove(int index, int count)
            {
                var end = index + (count - 1);

                if (_realized is not null && index <= _realizedIndex && end >= _realizedIndex)
                {
                    RecycleElement(_realized);
                    _realized = null;
                    _realizedIndex = -1;
                }
                else if (index < _realizedIndex)
                {
                    _realizedIndex -= count;
                }
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Add(e.NewStartingIndex, e.NewItems!.Count);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Remove(e.OldStartingIndex, e.OldItems!.Count);
                    break;
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                    Remove(e.OldStartingIndex, e.OldItems!.Count);
                    Add(e.NewStartingIndex, e.NewItems!.Count);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    if (_realized is not null)
                    {
                        RecycleElement(_realized);
                        _realized = null;
                        _realizedIndex = -1;
                    }
                    break;
            }

            InvalidateMeasure();
        }

        private Control GetOrCreateElement(ItemsSourceView items, int index)
        {
            return GetRealizedElement(index) ??
                GetItemIsOwnContainer(items, index) ??
                GetRecycledElement(items, index) ??
                CreateElement(items, index);
        }

        private Control? GetRealizedElement(int index)
        {
            return _realizedIndex == index ? _realized : null;
        }

        private Control? GetItemIsOwnContainer(ItemsSourceView items, int index)
        {
            Debug.Assert(ItemsControl is not null);

            if (items[index] is Control controlItem)
            {
                var generator = ItemsControl!.ItemContainerGenerator;

                if (controlItem.IsSet(ItemIsOwnContainerProperty))
                {
                    controlItem.IsVisible = true;
                    return controlItem;
                }
                else if (generator.IsItemItsOwnContainer(controlItem))
                {
                    AddInternalChild(controlItem);
                    generator.PrepareItemContainer(controlItem, controlItem, index);
                    controlItem.SetValue(ItemIsOwnContainerProperty, true);
                    return controlItem;
                }
            }

            return null;
        }

        private Control? GetRecycledElement(ItemsSourceView items, int index)
        {
            Debug.Assert(ItemsControl is not null);

            var generator = ItemsControl!.ItemContainerGenerator;
            var item = items[index];

            if (_recyclePool?.Count > 0)
            {
                var recycled = _recyclePool.Pop();
                recycled.IsVisible = true;
                generator.PrepareItemContainer(recycled, item, index);
                return recycled;
            }

            return null;
        }

        private Control CreateElement(ItemsSourceView items, int index)
        {
            Debug.Assert(ItemsControl is not null);

            var generator = ItemsControl!.ItemContainerGenerator;
            var item = items[index];
            var container = generator.CreateContainer();

            AddInternalChild(container);
            generator.PrepareItemContainer(container, item, index);

            return container;
        }

        private void RecycleElement(Control element)
        {
            Debug.Assert(ItemsControl is not null);

            if (element.IsSet(ItemIsOwnContainerProperty))
            {
                element.IsVisible = false;
            }
            else
            {
                ItemsControl!.ItemContainerGenerator.ClearItemContainer(element);
                _recyclePool ??= new();
                _recyclePool.Push(element);
                element.IsVisible = false;
            }
        }

        private IPageTransition? GetTransition() => (ItemsControl as Carousel)?.PageTransition;

        private void TransitionFinished(Task task)
        {
            if (task.IsCanceled)
                return;

            if (_transitionFrom is not null)
                RecycleElement(_transitionFrom);
            _transition = null;
            _transitionFrom = null;
            _transitionFromIndex = -1;
        }
    }
}
