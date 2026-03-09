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

        private static readonly StyledProperty<double> CompletionProgressProperty =
            AvaloniaProperty.Register<VirtualizingCarouselPanel, double>("CompletionProgress");

        private const double SwipeCommitThreshold = 0.25;
        private const double RubberBandFactor = 0.3;
        private const double MaxCompletionDuration = 0.35;
        private const double MinCompletionDuration = 0.12;
        private const double GestureDeadZone = 10;

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

        private IPointer? _gesturePointer;
        private bool _isDragging;
        private double _totalDelta;
        private bool _isForward;
        private Control? _swipeTarget;
        private int _swipeTargetIndex = -1;
        private bool _isRubberBanding;
        private Point _gestureStartPosition;
        private bool _gestureDirectionDetermined;

        private CancellationTokenSource? _completionCts;
        private double _completionEndProgress;

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
                {
                    InvalidateMeasure();
                }

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
                        {
                            RecycleElement(_transitionFrom);
                        }

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

            if (_swipeTarget is not null && (_isDragging || _completionCts is { IsCancellationRequested: false }))
            {
                _swipeTarget.Measure(finalSize);
                _swipeTarget.Arrange(new Rect(finalSize));
            }

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
            {
                return null;
            }

            if (index == _realizedIndex)
            {
                return _realized;
            }

            if (Items[index] is Control c && c.GetValue(RecycleKeyProperty) == s_itemIsItsOwnContainer)
            {
                return c;
            }

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

        /// <inheritdoc/>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == CompletionProgressProperty)
            {
                UpdateSwipeProgress(change.GetNewValue<double>(), Bounds.Width);
            }
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (!IsSwipeEnabled())
            {
                return;
            }

            if (_gesturePointer is not null || _realized is null)
            {
                return;
            }

            var props = e.GetCurrentPoint(this).Properties;
            if (!props.IsLeftButtonPressed)
            {
                return;
            }

            // Cancel any ongoing completion animation
            if (_completionCts is { IsCancellationRequested: false })
            {
                _completionCts.Cancel();

                var wasCommit = _completionEndProgress > 0.5;
                if (wasCommit && _swipeTarget is not null)
                {
                    if (_realized != null)
                    {
                        ResetVisualState(_realized);
                        RecycleElement(_realized);
                    }

                    _realized = _swipeTarget;
                    _realizedIndex = _swipeTargetIndex;
                    ResetVisualState(_realized);

                    _offset = new Vector(_swipeTargetIndex, 0);
                    if (ItemsControl is Carousel carousel)
                    {
                        carousel.SelectedIndex = _swipeTargetIndex;
                    }
                }
                else
                {
                    ResetSwipeState();
                }

                _swipeTarget = null;
                _swipeTargetIndex = -1;
                _totalDelta = 0;
            }

            _gesturePointer = e.Pointer;
            _gestureStartPosition = e.GetPosition(this);
            _gestureDirectionDetermined = false;
            _isDragging = false;
            _totalDelta = 0;
            _isForward = true;
            _swipeTarget = null;
            _swipeTargetIndex = -1;
            _isRubberBanding = false;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);

            if (_gesturePointer != e.Pointer || _realized is null)
            {
                return;
            }

            var pos = e.GetPosition(this);
            var deltaX = pos.X - _gestureStartPosition.X;

            if (!_gestureDirectionDetermined)
            {
                var deltaY = pos.Y - _gestureStartPosition.Y;

                if (Math.Abs(deltaX) < GestureDeadZone && Math.Abs(deltaY) < GestureDeadZone)
                {
                    return;
                }

                if (Math.Abs(deltaY) > Math.Abs(deltaX))
                {
                    ResetGestureTracking();
                    return;
                }

                _gestureDirectionDetermined = true;
                _isForward = deltaX < 0;
                _isRubberBanding = false;

                var targetIndex = _isForward ? _realizedIndex + 1 : _realizedIndex - 1;

                if (targetIndex < 0 || targetIndex >= Items.Count)
                {
                    _isRubberBanding = true;
                }

                // Cancel any existing page transition
                if (_transition is not null)
                {
                    _transition.Cancel();
                    _transition = null;
                    if (_transitionFrom is not null)
                    {
                        RecycleElement(_transitionFrom);
                    }

                    _transitionFrom = null;
                    _transitionFromIndex = -1;
                }

                _isDragging = true;
                _swipeTargetIndex = _isRubberBanding ? -1 : targetIndex;

                if (ItemsControl is Carousel carousel)
                {
                    carousel.IsSwiping = true;
                }

                if (!_isRubberBanding)
                {
                    _swipeTarget = GetOrCreateElement(Items, _swipeTargetIndex);
                    _swipeTarget.Measure(Bounds.Size);
                    _swipeTarget.Arrange(new Rect(Bounds.Size));
                    UpdateSwipeProgress(0, Bounds.Width);
                    _swipeTarget.IsVisible = true;
                }

                e.Pointer.Capture(this);
                e.Handled = true;
                _gestureStartPosition = pos;
                return;
            }

            if (!_isDragging)
            {
                return;
            }

            _totalDelta = deltaX;

            // Clamp so delta cannot cross zero
            if (_isForward)
            {
                _totalDelta = Math.Min(0, _totalDelta);
            }
            else
            {
                _totalDelta = Math.Max(0, _totalDelta);
            }

            var size = Bounds.Width;
            if (size <= 0)
            {
                return;
            }

            var rawProgress = Math.Clamp(Math.Abs(_totalDelta) / size, 0, 1);
            var progress = _isRubberBanding
                ? RubberBandFactor * Math.Sqrt(rawProgress)
                : rawProgress;

            UpdateSwipeProgress(progress, size);
            e.Handled = true;
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            if (_gesturePointer != e.Pointer)
            {
                return;
            }

            if (_isDragging && ItemsControl is Carousel carousel)
            {
                var size = Bounds.Width;
                var rawProgress = size > 0 ? Math.Abs(_totalDelta) / size : 0;
                var currentProgress = _isRubberBanding
                    ? RubberBandFactor * Math.Sqrt(rawProgress)
                    : rawProgress;

                var commit = !_isRubberBanding
                             && currentProgress >= SwipeCommitThreshold
                             && _swipeTarget is not null;

                _completionEndProgress = commit ? 1.0 : 0.0;

                var remainingDistance = Math.Abs(_completionEndProgress - currentProgress);
                var durationSeconds = Math.Clamp(remainingDistance * MaxCompletionDuration, MinCompletionDuration, MaxCompletionDuration);

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

                e.Handled = true;
            }

            _gesturePointer = null;
        }

        protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
        {
            base.OnPointerCaptureLost(e);

            if (_gesturePointer != e.Pointer)
            {
                return;
            }

            if (_isDragging)
            {
                ResetSwipeState();
            }

            _gesturePointer = null;
        }

        protected override void OnItemsChanged(IReadOnlyList<object?> items, NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(items, e);

            if (_isDragging || _completionCts is { IsCancellationRequested: false })
            {
                _completionCts?.Cancel();
                ResetSwipeState();
                _gesturePointer = null;
                _gestureDirectionDetermined = false;
            }

            void Add(int index, int count)
            {
                if (index <= _realizedIndex)
                {
                    _realizedIndex += count;
                }
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

        private async Task RunCompletionAnimation(Animation.Animation animation, Carousel carousel, CancellationToken cancellationToken)
        {
            await animation.RunAsync(this, null, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var commit = _completionEndProgress > 0.5;

            if (commit && _swipeTarget is not null)
            {
                var targetIndex = _swipeTargetIndex;
                var targetElement = _swipeTarget;

                if (_realized != null)
                {
                    ResetVisualState(_realized);
                    RecycleElement(_realized);
                }

                _realized = targetElement;
                _realizedIndex = targetIndex;
                ResetVisualState(_realized);

                _offset = new Vector(targetIndex, 0);
                carousel.SelectedIndex = targetIndex;

                _swipeTarget = null;
                _swipeTargetIndex = -1;
                _totalDelta = 0;
                _isRubberBanding = false;
                carousel.IsSwiping = false;
            }
            else
            {
                ResetSwipeState();
            }
        }

        private void ResetSwipeState()
        {
            if (ItemsControl is Carousel carousel)
            {
                carousel.IsSwiping = false;
            }

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
            _isRubberBanding = false;
        }

        private static void ResetVisualState(Control? control)
        {
            if (control is null)
            {
                return;
            }

            control.RenderTransform = null;
            control.Opacity = 1;
            control.ZIndex = 0;
            control.Clip = null;
        }

        private void ResetGestureTracking()
        {
            _gesturePointer = null;
            _isDragging = false;
            _gestureDirectionDetermined = false;
        }

        private void UpdateSwipeProgress(double progress, double size)
        {
            if (GetTransition() is IInteractivePageTransition interactive)
            {
                interactive.Update(progress, _realized, _isRubberBanding ? null : _swipeTarget, _isForward);
            }
            else
            {
                ApplyDefaultTransition(progress, size);
            }
        }

        private void ApplyDefaultTransition(double progress, double size)
        {
            var offset = size * progress;

            if (_realized != null)
            {
                if (_realized.RenderTransform is not TranslateTransform ft)
                {
                    _realized.RenderTransform = ft = new TranslateTransform();
                }

                ft.X = _isForward ? -offset : offset;
            }

            if (_swipeTarget != null && !_isRubberBanding)
            {
                _swipeTarget.IsVisible = true;
                if (_swipeTarget.RenderTransform is not TranslateTransform tt)
                {
                    _swipeTarget.RenderTransform = tt = new TranslateTransform();
                }

                tt.X = _isForward ? size - offset : -(size - offset);
            }
        }

        private bool IsSwipeEnabled() => (ItemsControl as Carousel)?.IsSwipeEnabled == true;

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
            {
                return null;
            }

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

            ResetVisualState(element);

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
            {
                return;
            }

            if (task.IsFaulted)
            {
                _ = task.Exception;
            }

            if (_transitionFrom is not null)
            {
                RecycleElement(_transitionFrom);
            }

            _transition = null;
            _transitionFrom = null;
            _transitionFromIndex = -1;
        }
    }
}
