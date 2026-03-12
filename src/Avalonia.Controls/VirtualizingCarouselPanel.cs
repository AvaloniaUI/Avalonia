using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Media;
using Avalonia.Styling;

namespace Avalonia.Controls
{
    /// <summary>
    /// A panel used by <see cref="Carousel"/> to display the current item.
    /// </summary>
    public class VirtualizingCarouselPanel : VirtualizingPanel, ILogicalScrollable
    {
        private static readonly AttachedProperty<object?> RecycleKeyProperty =
            AvaloniaProperty.RegisterAttached<VirtualizingCarouselPanel, Control, object?>("RecycleKey");

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
        private int _swipeGestureId;
        private bool _isDragging;
        private double _totalDelta;
        private bool _isForward;
        private Control? _swipeTarget;
        private int _swipeTargetIndex = -1;
        private PageSlide.SlideAxis? _swipeAxis;
        private PageSlide.SlideAxis _lockedAxis;

        private const double SwipeCommitThreshold = 0.25;
        private const double VelocityCommitThreshold = 800;
        private const double MinSwipeDistanceForVelocityCommit = 0.05;
        private const double RubberBandFactor = 0.3;
        private const double MaxCompletionDuration = 0.35;
        private const double MinCompletionDuration = 0.12;

        private static readonly StyledProperty<double> CompletionProgressProperty =
            AvaloniaProperty.Register<VirtualizingCarouselPanel, double>("CompletionProgress");

        private CancellationTokenSource? _completionCts;
        private Carousel? _completionCarousel;
        private double _completionEndProgress;
        private bool _isRubberBanding;

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

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            TeardownGestureRecognizer();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            // The panel can attach to the visual tree before it's attached to its Carousel.
            // Ensure swipe wiring exists once measure runs with a live ItemsControl.
            if (_swipeGestureRecognizer is null &&
                ItemsControl is Carousel carousel &&
                carousel.IsSwipeEnabled)
            {
                RefreshGestureRecognizer();
            }

            var items = Items;
            var index = (ItemsControl as Carousel)?.SelectedIndex ?? (int)_offset.X;

            if (index != _realizedIndex)
            {
                if (_realized is not null)
                {
                    var cancelTransition = _transition is not null;

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
                        RecycleElement(_realized);
                    }
                    else
                    {
                        _transitionFrom = _realized;
                        _transitionFromIndex = _realizedIndex;
                    }

                    _realized = null;
                    _realizedIndex = -1;
                }

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

                var forward = _realizedIndex > _transitionFromIndex;
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
                        goto case NotifyCollectionChangedAction.Reset;

                    Remove(e.OldStartingIndex, e.OldItems!.Count);
                    Add(e.NewStartingIndex, e.NewItems!.Count);
                    break;
                case NotifyCollectionChangedAction.Move:
                    if (e.OldStartingIndex < 0)
                        goto case NotifyCollectionChangedAction.Reset;

                    Remove(e.OldStartingIndex, e.OldItems!.Count);
                    var insertIndex = e.NewStartingIndex;

                    if (e.NewStartingIndex > e.OldStartingIndex)
                        insertIndex -= e.OldItems.Count - 1;

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

            var element = GetRealizedElement(index);

            if (element is null)
            {
                var item = items[index];
                var generator = ItemContainerGenerator!;

                if (generator.NeedsContainer(item, index, out var recycleKey))
                {
                    element = GetRecycledElement(item, index, recycleKey) ??
                        CreateElement(item, index, recycleKey);
                }
                else
                {
                    element = GetItemAsOwnContainer(item, index);
                }
            }

            return element;
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

            // Hide first so cleanup doesn't visibly snap transforms/opacity for a frame.
            element.IsVisible = false;
            ResetVisualState(element);

            if (recycleKey == s_itemIsItsOwnContainer)
            {
                return;
            }

            ItemContainerGenerator.ClearItemContainer(element);
            _recyclePool ??= new();

            if (!_recyclePool.TryGetValue(recycleKey, out var pool))
            {
                pool = new();
                _recyclePool.Add(recycleKey, pool);
            }

            pool.Push(element);
        }

        private IPageTransition? GetTransition() => (ItemsControl as Carousel)?.PageTransition;

        private void TransitionFinished(Task task)
        {
            if (task.IsCanceled)
                return;

            if (task.IsFaulted)
                _ = task.Exception;

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
                IsMouseEnabled = true,
            };

            GestureRecognizers.Add(_swipeGestureRecognizer);
            AddHandler(InputElement.SwipeGestureEvent, OnSwipeGesture);
            AddHandler(InputElement.SwipeGestureEndedEvent, OnSwipeGestureEnded);
        }

        private void TeardownGestureRecognizer()
        {
            _completionCts?.Cancel();

            if (_swipeGestureRecognizer is not null)
            {
                _swipeGestureRecognizer.IsEnabled = false;
                _swipeGestureRecognizer = null;
            }

            RemoveHandler(InputElement.SwipeGestureEvent, OnSwipeGesture);
            RemoveHandler(InputElement.SwipeGestureEndedEvent, OnSwipeGestureEnded);
            ResetSwipeState();
        }

        private void OnSwipeGesture(object? sender, SwipeGestureEventArgs e)
        {
            if (ItemsControl is not Carousel carousel || !carousel.IsSwipeEnabled)
                return;

            if (_completionCts is { IsCancellationRequested: false })
            {
                _completionCts.Cancel();

                var wasCommit = _completionEndProgress > 0.5;
                if (wasCommit && _swipeTarget is not null)
                {
                    if (_realized != null)
                        RecycleElement(_realized);

                    _realized = _swipeTarget;
                    _realizedIndex = _swipeTargetIndex;
                    carousel.SelectedIndex = _swipeTargetIndex;
                }
                else
                {
                    ResetSwipeState();
                }

                _swipeTarget = null;
                _swipeTargetIndex = -1;
                _totalDelta = 0;
            }

            if (_isDragging && e.Id != _swipeGestureId)
                return;

            if (!_isDragging)
            {
                _lockedAxis = _swipeAxis ?? (Math.Abs(e.Delta.X) >= Math.Abs(e.Delta.Y)
                    ? PageSlide.SlideAxis.Horizontal
                    : PageSlide.SlideAxis.Vertical);
            }

            var delta = _lockedAxis == PageSlide.SlideAxis.Horizontal ? e.Delta.X : e.Delta.Y;

            if (!_isDragging)
            {
                _isForward = delta > 0;
                _isRubberBanding = false;
                var currentIndex = _realizedIndex;
                var targetIndex = _isForward ? currentIndex + 1 : currentIndex - 1;

                if (targetIndex >= Items.Count)
                {
                    if (carousel.WrapSelection)
                        targetIndex = 0;
                    else
                        _isRubberBanding = true;
                }
                else if (targetIndex < 0)
                {
                    if (carousel.WrapSelection)
                        targetIndex = Items.Count - 1;
                    else
                        _isRubberBanding = true;
                }

                if (!_isRubberBanding && (targetIndex == currentIndex || targetIndex < 0 || targetIndex >= Items.Count))
                    return;

                _isDragging = true;
                _swipeGestureId = e.Id;
                _totalDelta = 0;
                _swipeTargetIndex = _isRubberBanding ? -1 : targetIndex;
                carousel.IsSwiping = true;

                if (_transition is not null)
                {
                    _transition.Cancel();
                    _transition = null;
                    if (_transitionFrom is not null)
                        RecycleElement(_transitionFrom);
                    _transitionFrom = null;
                    _transitionFromIndex = -1;
                }

                if (!_isRubberBanding)
                {
                    _swipeTarget = GetOrCreateElement(Items, _swipeTargetIndex);
                    _swipeTarget.Measure(Bounds.Size);
                    _swipeTarget.Arrange(new Rect(Bounds.Size));
                    _swipeTarget.IsVisible = true;
                }
            }

            _totalDelta += delta;

            if (_isForward)
                _totalDelta = Math.Max(0, _totalDelta);
            else
                _totalDelta = Math.Min(0, _totalDelta);

            var size = _lockedAxis == PageSlide.SlideAxis.Horizontal ? Bounds.Width : Bounds.Height;
            if (size <= 0)
                return;

            var rawProgress = Math.Clamp(Math.Abs(_totalDelta) / size, 0, 1);
            var progress = _isRubberBanding
                ? RubberBandFactor * Math.Sqrt(rawProgress)
                : rawProgress;

            if (GetTransition() is IInteractivePageTransition interactive)
            {
                var swipeTarget = _isRubberBanding || ReferenceEquals(_realized, _swipeTarget) ? null : _swipeTarget;
                interactive.Update(progress, _realized, swipeTarget, _isForward);
            }

            e.Handled = true;
        }

        private void OnSwipeGestureEnded(object? sender, SwipeGestureEndedEventArgs e)
        {
            if (!_isDragging || e.Id != _swipeGestureId || ItemsControl is not Carousel carousel)
                return;

            var size = _lockedAxis == PageSlide.SlideAxis.Horizontal ? Bounds.Width : Bounds.Height;
            var rawProgress = size > 0 ? Math.Abs(_totalDelta) / size : 0;
            var currentProgress = _isRubberBanding
                ? RubberBandFactor * Math.Sqrt(rawProgress)
                : rawProgress;
            var velocity = _lockedAxis == PageSlide.SlideAxis.Horizontal
                ? Math.Abs(e.Velocity.X)
                : Math.Abs(e.Velocity.Y);
            var commit = !_isRubberBanding &&
                (currentProgress >= SwipeCommitThreshold ||
                 (velocity > VelocityCommitThreshold && currentProgress >= MinSwipeDistanceForVelocityCommit)) &&
                _swipeTarget is not null;

            _completionEndProgress = commit ? 1.0 : 0.0;
            _completionCarousel = carousel;

            var remainingDistance = Math.Abs(_completionEndProgress - currentProgress);
            var durationSeconds = velocity > 0
                ? Math.Clamp(remainingDistance * size / velocity, MinCompletionDuration, MaxCompletionDuration)
                : MaxCompletionDuration;

            _completionCts?.Cancel();
            _completionCts = new CancellationTokenSource();

            SetValue(CompletionProgressProperty, currentProgress);

            var animation = new Animation.Animation
            {
                FillMode = FillMode.Forward,
                Easing = new QuadraticEaseOut(),
                Duration = TimeSpan.FromSeconds(durationSeconds),
                Children =
                {
                    new KeyFrame
                    {
                        Setters = { new Setter { Property = CompletionProgressProperty, Value = currentProgress } },
                        Cue = new Cue(0d)
                    },
                    new KeyFrame
                    {
                        Setters = { new Setter { Property = CompletionProgressProperty, Value = _completionEndProgress } },
                        Cue = new Cue(1d)
                    }
                }
            };

            _isDragging = false;

            _ = RunCompletionAnimation(animation, carousel, _completionCts.Token);
        }

        private async Task RunCompletionAnimation(Animation.Animation animation, Carousel carousel, CancellationToken cancellationToken)
        {
            await animation.RunAsync(this, null, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return;

            if (GetTransition() is IInteractivePageTransition interactive)
            {
                var swipeTarget = ReferenceEquals(_realized, _swipeTarget) ? null : _swipeTarget;
                interactive.Update(_completionEndProgress, _realized, swipeTarget, _isForward);
            }

            var commit = _completionEndProgress > 0.5;

            if (commit && _swipeTarget is not null)
            {
                var targetIndex = _swipeTargetIndex;
                var targetElement = _swipeTarget;

                _swipeTarget = null;
                _swipeTargetIndex = -1;

                if (_realized != null)
                    RecycleElement(_realized);

                _realized = targetElement;
                _realizedIndex = targetIndex;

                carousel.SelectedIndex = targetIndex;
            }
            else
            {
                ResetSwipeState();
            }

            _totalDelta = 0;
            _swipeTarget = null;
            _swipeTargetIndex = -1;
            _isRubberBanding = false;
            carousel.IsSwiping = false;
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == CompletionProgressProperty)
            {
                if (!_isDragging && _swipeTarget is null)
                    return;

                var progress = change.GetNewValue<double>();
                if (GetTransition() is IInteractivePageTransition interactive)
                {
                    var swipeTarget = ReferenceEquals(_realized, _swipeTarget) ? null : _swipeTarget;
                    interactive.Update(progress, _realized, swipeTarget, _isForward);
                }
            }
        }

        private void ResetSwipeState()
        {
            _completionCarousel = null;

            if (ItemsControl is Carousel carousel)
                carousel.IsSwiping = false;

            ResetVisualState(_realized);

            if (_swipeTarget is not null)
                RecycleElement(_swipeTarget);

            _isDragging = false;
            _totalDelta = 0;
            _swipeTarget = null;
            _swipeTargetIndex = -1;
            _isRubberBanding = false;
        }

        private static void ResetVisualState(Control? control)
        {
            if (control is null)
                return;
            control.RenderTransform = null;
            control.Opacity = 1;
            control.ZIndex = 0;
            control.Clip = null;
        }
    }
}
