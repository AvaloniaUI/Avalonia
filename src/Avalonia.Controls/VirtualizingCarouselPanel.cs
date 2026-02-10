using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Media;
using Avalonia.Threading;

namespace Avalonia.Controls
{
    /// <summary>
    /// A panel used by <see cref="Carousel"/> to display the current item.
    /// </summary>
    public class VirtualizingCarouselPanel : VirtualizingPanel, ILogicalScrollable
    {
        private static readonly AttachedProperty<object?> RecycleKeyProperty =
            AvaloniaProperty.RegisterAttached<VirtualizingStackPanel, Control, object?>("RecycleKey");

        private static readonly object s_itemIsItsOwnContainer = new object();
        private Size _extent;
        private Vector _offset;
        private Size _viewport;
        private Dictionary<object, Stack<Control>>? _recyclePool;
        private Control? _realized;
        private int _realizedIndex = -1;
        private Control? _transitionFrom;
        private int _transitionFromIndex = -1;
        private CancellationTokenSource? _transition;
        private EventHandler? _scrollInvalidated;
        private bool _canHorizontallyScroll;
        private bool _canVerticallyScroll;

        private SwipeGestureRecognizer? _swipeGestureRecognizer;
        private bool _isDragging;
        private double _totalDelta;
        private bool _isForward;
        private Control? _swipeTarget;
        private int _swipeTargetIndex = -1;
        private PageSlide.SlideAxis? _swipeAxis;
        private PageSlide.SlideAxis _lockedAxis;

        private const double SwipeCommitThreshold = 0.25;

        private DispatcherTimer? _completionTimer;
        private double _completionStartProgress;
        private double _completionEndProgress;
        private DateTime _completionStartTime;
        private static readonly TimeSpan CompletionDuration = TimeSpan.FromMilliseconds(250);


        bool ILogicalScrollable.CanHorizontallyScroll
        {
            get => _canHorizontallyScroll;
            set => _canHorizontallyScroll = value;
        }

        bool ILogicalScrollable.CanVerticallyScroll
        {
            get => _canVerticallyScroll;
            set => _canVerticallyScroll = value;
        }

        bool IScrollable.CanHorizontallyScroll => _canHorizontallyScroll;
        bool IScrollable.CanVerticallyScroll => _canVerticallyScroll;
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

        bool ILogicalScrollable.BringIntoView(Control target, Rect targetRect) => false;
        Control? ILogicalScrollable.GetControlInDirection(NavigationDirection direction, Control? from) => null;
        void ILogicalScrollable.RaiseScrollInvalidated(EventArgs e) => _scrollInvalidated?.Invoke(this, e);

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            RefreshGestureRecognizer();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var items = Items;
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

                var forward = (_realizedIndex > _transitionFromIndex);
                if (Items.Count > 2)
                {
                    forward = forward || (_transitionFromIndex == Items.Count - 1 && _realizedIndex == 0);
                    forward = forward && !(_transitionFromIndex == 0 && _realizedIndex == Items.Count - 1);
                }

                transition.Start(_transitionFrom, to, forward, _transition.Token)
                    .ContinueWith(TransitionFinished, TaskScheduler.FromCurrentSynchronizationContext());
            }

            return result;
        }

        protected override IInputElement? GetControl(NavigationDirection direction, IInputElement? from, bool wrap) => null;

        protected internal override Control? ContainerFromIndex(int index)
        {
            if (index < 0 || index >= Items.Count)
                return null;
            if (index == _realizedIndex)
                return _realized;
            if (Items[index] is Control c && c.GetValue(RecycleKeyProperty) == s_itemIsItsOwnContainer)
                return c;
            return null;
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
                    if (e.OldStartingIndex < 0)
                    {
                        goto case NotifyCollectionChangedAction.Reset;
                    }

                    Remove(e.OldStartingIndex, e.OldItems!.Count);
                    Add(e.NewStartingIndex, e.NewItems!.Count);
                    break;
                case NotifyCollectionChangedAction.Move:
                    if (e.OldStartingIndex < 0)
                    {
                        goto case NotifyCollectionChangedAction.Reset;
                    }

                    Remove(e.OldStartingIndex, e.OldItems!.Count);
                    var insertIndex = e.NewStartingIndex;

                    if (e.NewStartingIndex > e.OldStartingIndex)
                    {
                        insertIndex -= e.OldItems.Count - 1;
                    }

                    Add(insertIndex, e.NewItems!.Count);
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

        private Control GetOrCreateElement(IReadOnlyList<object?> items, int index)
        {
            Debug.Assert(ItemContainerGenerator is not null);

            var e = GetRealizedElement(index);

            if (e is null)
            {
                var item = items[index];
                var generator = ItemContainerGenerator!;

                if (generator.NeedsContainer(item, index, out var recycleKey))
                {
                    e = GetRecycledElement(item, index, recycleKey) ??
                        CreateElement(item, index, recycleKey);
                }
                else
                {
                    e = GetItemAsOwnContainer(item, index);
                }
            }

            return e;
        }

        private Control? GetRealizedElement(int index)
        {
            return _realizedIndex == index ? _realized : null;
        }

        private Control GetItemAsOwnContainer(object? item, int index)
        {
            Debug.Assert(ItemContainerGenerator is not null);

            var controlItem = (Control)item!;
            var generator = ItemContainerGenerator!;

            if (!controlItem.IsSet(RecycleKeyProperty))
            {
                generator.PrepareItemContainer(controlItem, controlItem, index);
                AddInternalChild(controlItem);
                controlItem.SetValue(RecycleKeyProperty, s_itemIsItsOwnContainer);
                generator.ItemContainerPrepared(controlItem, item, index);
            }

            controlItem.IsVisible = true;
            return controlItem;
        }

        private Control? GetRecycledElement(object? item, int index, object? recycleKey)
        {
            Debug.Assert(ItemContainerGenerator is not null);

            if (recycleKey is null)
                return null;

            var generator = ItemContainerGenerator!;

            if (_recyclePool?.TryGetValue(recycleKey, out var recyclePool) == true && recyclePool.Count > 0)
            {
                var recycled = recyclePool.Pop();
                recycled.IsVisible = true;
                generator.PrepareItemContainer(recycled, item, index);
                generator.ItemContainerPrepared(recycled, item, index);
                return recycled;
            }

            return null;
        }

        private Control CreateElement(object? item, int index, object? recycleKey)
        {
            Debug.Assert(ItemContainerGenerator is not null);

            var generator = ItemContainerGenerator!;
            var container = generator.CreateContainer(item, index, recycleKey);

            container.SetValue(RecycleKeyProperty, recycleKey);
            generator.PrepareItemContainer(container, item, index);
            AddInternalChild(container);
            generator.ItemContainerPrepared(container, item, index);

            return container;
        }

        private void RecycleElement(Control element)
        {
            Debug.Assert(ItemContainerGenerator is not null);

            var recycleKey = element.GetValue(RecycleKeyProperty);
            Debug.Assert(recycleKey is not null);

            if (recycleKey == s_itemIsItsOwnContainer)
            {
                element.IsVisible = false;
            }
            else
            {
                ItemContainerGenerator.ClearItemContainer(element);
                _recyclePool ??= new();

                if (!_recyclePool.TryGetValue(recycleKey, out var pool))
                {
                    pool = new();
                    _recyclePool.Add(recycleKey, pool);
                }

                pool.Push(element);
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

        /// <summary>
        /// Refreshes the gesture recognizer based on the carousel's IsSwipeEnabled and PageTransition settings.
        /// </summary>
        internal void RefreshGestureRecognizer()
        {
            TeardownGestureRecognizer();

            if (ItemsControl is not Carousel carousel || !carousel.IsSwipeEnabled)
                return;

            _swipeAxis = carousel.GetTransitionAxis();

            _swipeGestureRecognizer = new SwipeGestureRecognizer
            {
                CanHorizontallySwipe = _swipeAxis != PageSlide.SlideAxis.Vertical,
                CanVerticallySwipe = _swipeAxis != PageSlide.SlideAxis.Horizontal,
            };

            GestureRecognizers.Add(_swipeGestureRecognizer);
            AddHandler(Gestures.SwipeGestureEvent, OnSwipeGesture);
            AddHandler(Gestures.SwipeGestureEndedEvent, OnSwipeGestureEnded);
        }

        private void TeardownGestureRecognizer()
        {
            if (_swipeGestureRecognizer is not null)
            {
                GestureRecognizers.Remove(_swipeGestureRecognizer);
                _swipeGestureRecognizer = null;
            }

            RemoveHandler(Gestures.SwipeGestureEvent, OnSwipeGesture);
            RemoveHandler(Gestures.SwipeGestureEndedEvent, OnSwipeGestureEnded);
            ResetSwipeState();
        }

        private void OnSwipeGesture(object? sender, SwipeGestureEventArgs e)
        {
            if (ItemsControl is not Carousel carousel || !carousel.IsSwipeEnabled)
                return;

            if (!_isDragging)
            {
                // Lock the axis on gesture start. Use the configured axis, or detect from the first delta
                _lockedAxis = _swipeAxis ?? (Math.Abs(e.Delta.X) >= Math.Abs(e.Delta.Y) ?
                    PageSlide.SlideAxis.Horizontal :
                    PageSlide.SlideAxis.Vertical);
            }

            var delta = _lockedAxis == PageSlide.SlideAxis.Horizontal ? e.Delta.X : e.Delta.Y;

            if (!_isDragging)
            {
                _isForward = delta > 0;
                var currentIndex = _realizedIndex;
                var targetIndex = _isForward ? currentIndex + 1 : currentIndex - 1;

                // Handle wrapping and boundary check
                if (targetIndex >= Items.Count)
                {
                    if (carousel.WrapSelection)
                        targetIndex = 0;
                    else
                        return;
                }
                else if (targetIndex < 0)
                {
                    if (carousel.WrapSelection)
                        targetIndex = Items.Count - 1;
                    else
                        return;
                }

                if (targetIndex == currentIndex || targetIndex < 0 || targetIndex >= Items.Count)
                    return;

                _isDragging = true;
                _totalDelta = 0;
                _swipeTargetIndex = targetIndex;
                carousel.IsSwiping = true;

                // Cancel any running transition
                if (_transition is not null)
                {
                    _transition.Cancel();
                    _transition = null;
                    if (_transitionFrom is not null)
                        RecycleElement(_transitionFrom);
                    _transitionFrom = null;
                    _transitionFromIndex = -1;
                }

                // Realize the target item
                _swipeTarget = GetOrCreateElement(Items, _swipeTargetIndex);
                _swipeTarget.Measure(Bounds.Size);
                _swipeTarget.Arrange(new Rect(Bounds.Size));
                _swipeTarget.IsVisible = true;
            }

            _totalDelta += delta;

            // If direction changed, reset the gesture
            if ((_isForward && _totalDelta < 0) || (!_isForward && _totalDelta > 0))
            {
                ResetSwipeState();
                return;
            }
            
            var size = _lockedAxis == PageSlide.SlideAxis.Horizontal ? Bounds.Width : Bounds.Height;
            if (size <= 0) return;

            var progress = Math.Clamp(Math.Abs(_totalDelta) / size, 0, 1);

            // Drive the interactive transition if supported; otherwise swipe still navigates on release
            if (GetTransition() is IInteractivePageTransition interactive)
            {
                interactive.Update(progress, _realized, _swipeTarget, _isForward, _lockedAxis, Bounds.Size);
            }

            e.Handled = true;
        }

        private void OnSwipeGestureEnded(object? sender, SwipeGestureEndedEventArgs e)
        {
            if (!_isDragging || ItemsControl is not Carousel carousel)
                return;

            var size = _lockedAxis == PageSlide.SlideAxis.Horizontal ? Bounds.Width : Bounds.Height;
            var currentProgress = size > 0 ? Math.Abs(_totalDelta) / size : 0;
            var commit = currentProgress >= SwipeCommitThreshold && _swipeTarget is not null;

            _completionStartProgress = currentProgress;
            _completionEndProgress = commit ? 1.0 : 0.0;
            _completionStartTime = DateTime.UtcNow;

            _completionTimer?.Stop();
            _completionTimer = new DispatcherTimer(
                TimeSpan.FromMilliseconds(16),
                DispatcherPriority.Render,
                (s, e) => OnCompletionTimerTick(carousel));
            _completionTimer.Start();

            _isDragging = false;
        }

        private void OnCompletionTimerTick(Carousel carousel)
        {
            var elapsed = DateTime.UtcNow - _completionStartTime;
            var ratio = Math.Min(1.0, elapsed.TotalMilliseconds / CompletionDuration.TotalMilliseconds);
            
            var easedRatio = 1 - Math.Pow(1 - ratio, 3);
            var progress = _completionStartProgress + (_completionEndProgress - _completionStartProgress) * easedRatio;

            if (GetTransition() is IInteractivePageTransition interactive)
            {
                interactive.Update(progress, _realized, _swipeTarget, _isForward, _lockedAxis, Bounds.Size);
            }

            if (ratio >= 1.0)
            {
                _completionTimer?.Stop();
                _completionTimer = null;

                var commit = _completionEndProgress > 0.5;

                if (commit && _swipeTarget is not null)
                {
                    // Snap to the new state
                    var targetIndex = _swipeTargetIndex;
                    var targetElement = _swipeTarget;

                    // Swap the realized element before setting SelectedIndex
                    // to prevent MeasureOverride from starting a NEW transition.
                    if (_realized != null)
                    {
                        ResetVisualState(_realized);
                        _realized.IsVisible = false;
                        RecycleElement(_realized);
                    }

                    _realized = targetElement;
                    _realizedIndex = targetIndex;
                    ResetVisualState(_realized);
                    
                    carousel.SelectedIndex = targetIndex;
                }
                else
                {
                    // Snap back
                    ResetVisualState(_realized);
                    if (_swipeTarget is not null)
                    {
                        ResetVisualState(_swipeTarget);
                        RecycleElement(_swipeTarget);
                    }
                }

                _totalDelta = 0;
                _swipeTarget = null;
                _swipeTargetIndex = -1;
                carousel.IsSwiping = false;
            }
        }

        private void ResetSwipeState()
        {
            if (ItemsControl is Carousel carousel)
                carousel.IsSwiping = false;

            ResetVisualState(_realized);

            if (_swipeTarget is not null)
            {
                ResetVisualState(_swipeTarget);
                RecycleElement(_swipeTarget);
            }

            _isDragging = false;
            _totalDelta = 0;
            _swipeTarget = null;
            _swipeTargetIndex = -1;
        }

        private static void ResetVisualState(Control? control)
        {
            if (control is null) return;
            control.RenderTransform = null;
            control.Opacity = 1;
            control.ZIndex = 0;
        }
    }
}
