using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    /// <summary>
    /// A panel used by <see cref="Carousel"/> to display the current item.
    /// </summary>
    public class VirtualizingCarouselPanel : VirtualizingPanel, ILogicalScrollable
    {
        private sealed class ViewportRealizedItem
        {
            public ViewportRealizedItem(int itemIndex, Control control)
            {
                ItemIndex = itemIndex;
                Control = control;
            }

            public int ItemIndex { get; }
            public Control Control { get; }
        }

        private static readonly AttachedProperty<object?> RecycleKeyProperty =
            AvaloniaProperty.RegisterAttached<VirtualizingCarouselPanel, Control, object?>("RecycleKey");

        private static readonly object s_itemIsItsOwnContainer = new object();
        private Size _extent;
        private Vector _offset;
        private Size _viewport;
        private Dictionary<object, Stack<Control>>? _recyclePool;
        private readonly Dictionary<int, ViewportRealizedItem> _viewportRealized = new();
        private Control? _realized;
        private int _realizedIndex = -1;
        private Control? _transitionFrom;
        private int _transitionFromIndex = -1;
        private CancellationTokenSource? _transition;
        private Task? _transitionTask;
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
        private const double RubberBandReturnDuration = 0.16;
        private const double MaxCompletionDuration = 0.35;
        private const double MinCompletionDuration = 0.12;

        private static readonly StyledProperty<double> CompletionProgressProperty =
            AvaloniaProperty.Register<VirtualizingCarouselPanel, double>("CompletionProgress");
        private static readonly StyledProperty<double> OffsetAnimationProgressProperty =
            AvaloniaProperty.Register<VirtualizingCarouselPanel, double>("OffsetAnimationProgress");

        private CancellationTokenSource? _completionCts;
        private CancellationTokenSource? _offsetAnimationCts;
        private double _completionEndProgress;
        private bool _isRubberBanding;
        private double _dragStartOffset;
        private double _progressStartOffset;
        private double _offsetAnimationStart;
        private double _offsetAnimationTarget;
        private double _activeViewportTargetOffset;
        private int _progressFromIndex = -1;
        private int _progressToIndex = -1;

        internal bool IsManagingInteractionOffset =>
            UsesViewportFractionLayout() &&
            (_isDragging || _offsetAnimationCts is { IsCancellationRequested: false });

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
            set => SetOffset(value);
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

        private bool UsesViewportFractionLayout()
        {
            return ItemsControl is Carousel carousel &&
                   !MathUtilities.AreClose(carousel.ViewportFraction, 1d);
        }

        private PageSlide.SlideAxis GetLayoutAxis()
        {
            return (ItemsControl as Carousel)?.GetLayoutAxis() ?? PageSlide.SlideAxis.Horizontal;
        }

        private double GetViewportFraction()
        {
            return (ItemsControl as Carousel)?.ViewportFraction ?? 1d;
        }

        private double GetViewportUnits()
        {
            return 1d / GetViewportFraction();
        }

        private double GetPrimaryOffset(Vector offset)
        {
            return GetLayoutAxis() == PageSlide.SlideAxis.Vertical ? offset.Y : offset.X;
        }

        private double GetPrimarySize(Size size)
        {
            return GetLayoutAxis() == PageSlide.SlideAxis.Vertical ? size.Height : size.Width;
        }

        private double GetCrossSize(Size size)
        {
            return GetLayoutAxis() == PageSlide.SlideAxis.Vertical ? size.Width : size.Height;
        }

        private Size CreateLogicalSize(double primary)
        {
            return GetLayoutAxis() == PageSlide.SlideAxis.Vertical ?
                new Size(1, primary) :
                new Size(primary, 1);
        }

        private Size CreateItemSize(double primary, double cross)
        {
            return GetLayoutAxis() == PageSlide.SlideAxis.Vertical ?
                new Size(cross, primary) :
                new Size(primary, cross);
        }

        private Rect CreateItemRect(double primaryOffset, double primarySize, double crossSize)
        {
            return GetLayoutAxis() == PageSlide.SlideAxis.Vertical ?
                new Rect(0, primaryOffset, crossSize, primarySize) :
                new Rect(primaryOffset, 0, primarySize, crossSize);
        }

        private Vector WithPrimaryOffset(Vector offset, double primary)
        {
            return GetLayoutAxis() == PageSlide.SlideAxis.Vertical ?
                new Vector(offset.X, primary) :
                new Vector(primary, offset.Y);
        }

        private Size ResolveLayoutSize(Size availableSize)
        {
            var owner = ItemsControl as Control;

            double ResolveDimension(double available, double bounds, double ownerBounds, double ownerExplicit)
            {
                if (!double.IsInfinity(available) && available > 0)
                    return available;

                if (bounds > 0)
                    return bounds;

                if (ownerBounds > 0)
                    return ownerBounds;

                return double.IsNaN(ownerExplicit) ? 0 : ownerExplicit;
            }

            var width = ResolveDimension(availableSize.Width, Bounds.Width, owner?.Bounds.Width ?? 0, owner?.Width ?? double.NaN);
            var height = ResolveDimension(availableSize.Height, Bounds.Height, owner?.Bounds.Height ?? 0, owner?.Height ?? double.NaN);
            return new Size(width, height);
        }

        private double GetViewportItemExtent(Size size)
        {
            var viewportUnits = GetViewportUnits();
            return viewportUnits <= 0 ? 0 : GetPrimarySize(size) / viewportUnits;
        }

        private bool UsesViewportWrapLayout()
        {
            return UsesViewportFractionLayout() &&
                   ItemsControl is Carousel { WrapSelection: true } &&
                   Items.Count > 1;
        }

        private static int NormalizeIndex(int index, int count)
        {
            return ((index % count) + count) % count;
        }

        private double GetNearestLogicalOffset(int itemIndex, double referenceOffset)
        {
            if (!UsesViewportWrapLayout() || Items.Count == 0)
                return Math.Clamp(itemIndex, 0, Math.Max(0, Items.Count - 1));

            var wrapSpan = Items.Count;
            var wrapMultiplier = Math.Round((referenceOffset - itemIndex) / wrapSpan);
            return itemIndex + (wrapMultiplier * wrapSpan);
        }

        private bool IsPreferredViewportSlot(int candidateLogicalIndex, int existingLogicalIndex, double primaryOffset)
        {
            var candidateDistance = Math.Abs(candidateLogicalIndex - primaryOffset);
            var existingDistance = Math.Abs(existingLogicalIndex - primaryOffset);

            if (!MathUtilities.AreClose(candidateDistance, existingDistance))
                return candidateDistance < existingDistance;

            var candidateInRange = candidateLogicalIndex >= 0 && candidateLogicalIndex < Items.Count;
            var existingInRange = existingLogicalIndex >= 0 && existingLogicalIndex < Items.Count;

            if (candidateInRange != existingInRange)
                return candidateInRange;

            if (_isDragging)
                return _isForward ? candidateLogicalIndex > existingLogicalIndex : candidateLogicalIndex < existingLogicalIndex;

            return candidateLogicalIndex < existingLogicalIndex;
        }

        private IReadOnlyList<(int LogicalIndex, int ItemIndex)> GetRequiredViewportSlots(double primaryOffset)
        {
            if (Items.Count == 0)
                return Array.Empty<(int LogicalIndex, int ItemIndex)>();

            var viewportUnits = GetViewportUnits();
            var edgeInset = (viewportUnits - 1) / 2;
            var start = (int)Math.Floor(primaryOffset - edgeInset);
            var end = (int)Math.Ceiling(primaryOffset + viewportUnits - edgeInset) - 1;

            if (!UsesViewportWrapLayout())
            {
                start = Math.Max(0, start);
                end = Math.Min(Items.Count - 1, end);

                if (start > end)
                    return Array.Empty<(int LogicalIndex, int ItemIndex)>();

                var result = new (int LogicalIndex, int ItemIndex)[end - start + 1];

                for (var i = 0; i < result.Length; ++i)
                {
                    var index = start + i;
                    result[i] = (index, index);
                }

                return result;
            }

            var bestSlots = new Dictionary<int, int>();

            for (var logicalIndex = start; logicalIndex <= end; ++logicalIndex)
            {
                var itemIndex = NormalizeIndex(logicalIndex, Items.Count);

                if (!bestSlots.TryGetValue(itemIndex, out var existingLogicalIndex) ||
                    IsPreferredViewportSlot(logicalIndex, existingLogicalIndex, primaryOffset))
                {
                    bestSlots[itemIndex] = logicalIndex;
                }
            }

            return bestSlots
                .Select(x => (LogicalIndex: x.Value, ItemIndex: x.Key))
                .OrderBy(x => x.LogicalIndex)
                .ToArray();
        }

        private bool ViewportSlotsChanged(double oldPrimaryOffset, double newPrimaryOffset)
        {
            var oldSlots = GetRequiredViewportSlots(oldPrimaryOffset);
            var newSlots = GetRequiredViewportSlots(newPrimaryOffset);

            if (oldSlots.Count != newSlots.Count)
                return true;

            for (var i = 0; i < oldSlots.Count; ++i)
            {
                if (oldSlots[i].LogicalIndex != newSlots[i].LogicalIndex ||
                    oldSlots[i].ItemIndex != newSlots[i].ItemIndex)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetOffset(Vector value)
        {
            if (UsesViewportFractionLayout())
            {
                var oldPrimaryOffset = GetPrimaryOffset(_offset);
                var newPrimaryOffset = GetPrimaryOffset(value);

                if (MathUtilities.AreClose(oldPrimaryOffset, newPrimaryOffset))
                {
                    _offset = value;
                    return;
                }

                _offset = value;

                var rangeChanged = ViewportSlotsChanged(oldPrimaryOffset, newPrimaryOffset);

                if (rangeChanged)
                    InvalidateMeasure();
                else
                    InvalidateArrange();

                _scrollInvalidated?.Invoke(this, EventArgs.Empty);
                return;
            }

            if ((int)_offset.X != value.X)
                InvalidateMeasure();

            _offset = value;
        }

        private void ClearViewportRealized()
        {
            if (_viewportRealized.Count == 0)
                return;

            foreach (var element in _viewportRealized.Values.Select(x => x.Control).ToArray())
                RecycleElement(element);

            _viewportRealized.Clear();
        }

        private void ResetSinglePageState()
        {
            _transition?.Cancel();
            _transition = null;
            _transitionTask = null;

            if (_transitionFrom is not null)
                RecycleElement(_transitionFrom);

            if (_swipeTarget is not null)
                RecycleElement(_swipeTarget);

            if (_realized is not null)
                RecycleElement(_realized);

            _transitionFrom = null;
            _transitionFromIndex = -1;
            _swipeTarget = null;
            _swipeTargetIndex = -1;
            _realized = null;
            _realizedIndex = -1;
        }

        private void CancelOffsetAnimation()
        {
            _offsetAnimationCts?.Cancel();
            _offsetAnimationCts = null;
        }

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

        protected override void OnItemsControlChanged(ItemsControl? oldValue)
        {
            base.OnItemsControlChanged(oldValue);

            RefreshGestureRecognizer();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (UsesViewportFractionLayout())
                return MeasureViewportFractionOverride(availableSize);

            ClearViewportRealized();
            CancelOffsetAnimation();

            return MeasureSinglePageOverride(availableSize);
        }

        private Size MeasureSinglePageOverride(Size availableSize)
        {
            var items = Items;
            var index = (int)_offset.X;

            CompleteFinishedTransitionIfNeeded();

            if (index != _realizedIndex)
            {
                if (_realized is not null)
                {
                    // Cancel any already running transition, and recycle the element we're transitioning from.
                    if (_transition is not null)
                    {
                        _transition.Cancel();
                        _transition = null;
                        _transitionTask = null;
                        if (_transitionFrom is not null)
                            RecycleElement(_transitionFrom);
                        _transitionFrom = null;
                        _transitionFromIndex = -1;
                        ResetTransitionState(_realized);
                    }

                    if (GetTransition() is null)
                    {
                        RecycleElement(_realized);
                    }
                    else
                    {
                        // Record the current element as the element we're transitioning
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
            if (UsesViewportFractionLayout())
                return ArrangeViewportFractionOverride(finalSize);

            return ArrangeSinglePageOverride(finalSize);
        }

        private Size ArrangeSinglePageOverride(Size finalSize)
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

                _transitionTask = RunTransitionAsync(_transition, _transitionFrom, to, forward, transition);
            }

            return result;
        }

        private Size MeasureViewportFractionOverride(Size availableSize)
        {
            ResetSinglePageState();

            if (Items.Count == 0)
            {
                ClearViewportRealized();
                Extent = Viewport = new(0, 0);
                return default;
            }

            var layoutSize = ResolveLayoutSize(availableSize);
            var primarySize = GetPrimarySize(layoutSize);
            var crossSize = GetCrossSize(layoutSize);
            var viewportUnits = GetViewportUnits();

            if (primarySize <= 0 || viewportUnits <= 0)
            {
                ClearViewportRealized();
                Extent = Viewport = new(0, 0);
                return default;
            }

            var itemPrimarySize = primarySize / viewportUnits;
            var itemSize = CreateItemSize(itemPrimarySize, crossSize);
            var requiredSlots = GetRequiredViewportSlots(GetPrimaryOffset(_offset));
            var requiredMap = requiredSlots.ToDictionary(x => x.LogicalIndex, x => x.ItemIndex);

            foreach (var entry in _viewportRealized.ToArray())
            {
                if (!requiredMap.TryGetValue(entry.Key, out var itemIndex) ||
                    entry.Value.ItemIndex != itemIndex)
                {
                    RecycleElement(entry.Value.Control);
                    _viewportRealized.Remove(entry.Key);
                }
            }

            foreach (var slot in requiredSlots)
            {
                if (!_viewportRealized.ContainsKey(slot.LogicalIndex))
                {
                    _viewportRealized[slot.LogicalIndex] = new ViewportRealizedItem(
                        slot.ItemIndex,
                        GetOrCreateElement(Items, slot.ItemIndex));
                }
            }

            var maxCrossDesiredSize = 0d;

            foreach (var element in _viewportRealized.Values.Select(x => x.Control))
            {
                element.Measure(itemSize);
                maxCrossDesiredSize = Math.Max(maxCrossDesiredSize, GetCrossSize(element.DesiredSize));
            }

            Viewport = CreateLogicalSize(viewportUnits);
            Extent = CreateLogicalSize(Math.Max(0, Items.Count + viewportUnits - 1));

            var desiredPrimary = double.IsInfinity(primarySize) ? itemPrimarySize * viewportUnits : primarySize;
            var desiredCross = double.IsInfinity(crossSize) ? maxCrossDesiredSize : crossSize;
            return CreateItemSize(desiredPrimary, desiredCross);
        }

        private Size ArrangeViewportFractionOverride(Size finalSize)
        {
            var primarySize = GetPrimarySize(finalSize);
            var crossSize = GetCrossSize(finalSize);
            var viewportUnits = GetViewportUnits();

            if (primarySize <= 0 || viewportUnits <= 0)
                return finalSize;

            if (_viewportRealized.Count == 0 && Items.Count > 0)
            {
                InvalidateMeasure();
                return finalSize;
            }

            var itemPrimarySize = primarySize / viewportUnits;
            var edgeInset = (viewportUnits - 1) / 2;
            var primaryOffset = GetPrimaryOffset(_offset);

            foreach (var entry in _viewportRealized.OrderBy(x => x.Key))
            {
                var itemOffset = (edgeInset + entry.Key - primaryOffset) * itemPrimarySize;
                var rect = CreateItemRect(itemOffset, itemPrimarySize, crossSize);
                entry.Value.Control.IsVisible = true;
                entry.Value.Control.Arrange(rect);
            }

            return finalSize;
        }

        protected override IInputElement? GetControl(NavigationDirection direction, IInputElement? from, bool wrap) => null;

        protected internal override Control? ContainerFromIndex(int index)
        {
            if (index < 0 || index >= Items.Count)
                return null;
            var viewportRealized = _viewportRealized.Values.FirstOrDefault(x => x.ItemIndex == index);
            if (viewportRealized is not null)
                return viewportRealized.Control;
            if (index == _realizedIndex)
                return _realized;
            if (Items[index] is Control c && c.GetValue(RecycleKeyProperty) == s_itemIsItsOwnContainer)
                return c;
            return null;
        }

        protected internal override IEnumerable<Control>? GetRealizedContainers()
        {
            if (_viewportRealized.Count > 0)
                return _viewportRealized.OrderBy(x => x.Key).Select(x => x.Value.Control);

            return _realized is not null ? new[] { _realized } : null;
        }

        protected internal override int IndexFromContainer(Control container)
        {
            foreach (var entry in _viewportRealized)
            {
                if (ReferenceEquals(entry.Value.Control, container))
                    return entry.Value.ItemIndex;
            }

            return container == _realized ? _realizedIndex : -1;
        }

        protected internal override Control? ScrollIntoView(int index)
        {
            return null;
        }

        protected override void OnItemsChanged(IReadOnlyList<object?> items, NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(items, e);

            if (UsesViewportFractionLayout() || _viewportRealized.Count > 0)
            {
                ClearViewportRealized();
                InvalidateMeasure();
                return;
            }

            void Add(int index, int count)
            {
                if (_realized is null)
                {
                    InvalidateMeasure();
                    return;
                }

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
            var viewportRealized = _viewportRealized.Values.FirstOrDefault(x => x.ItemIndex == index);
            if (viewportRealized is not null)
                return viewportRealized.Control;

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
            ResetTransitionState(element);

            if (recycleKey == s_itemIsItsOwnContainer)
            {
                return;
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
            }
        }

        private IPageTransition? GetTransition() => (ItemsControl as Carousel)?.PageTransition;

        private void CompleteFinishedTransitionIfNeeded()
        {
            if (_transition is not null && _transitionTask?.IsCompleted == true)
            {
                if (_transitionFrom is not null)
                    RecycleElement(_transitionFrom);

                _transition = null;
                _transitionTask = null;
                _transitionFrom = null;
                _transitionFromIndex = -1;
            }
        }

        private async Task RunTransitionAsync(
            CancellationTokenSource transitionCts,
            Control transitionFrom,
            Control transitionTo,
            bool forward,
            IPageTransition transition)
        {
            try
            {
                await transition.Start(transitionFrom, transitionTo, forward, transitionCts.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected when a transition is interrupted by a newer navigation action.
            }
            catch (Exception e)
            {
                _ = e;
            }

            if (transitionCts.IsCancellationRequested || !ReferenceEquals(_transition, transitionCts))
                return;

            if (_transitionFrom is not null)
                RecycleElement(_transitionFrom);
            _transition = null;
            _transitionTask = null;
            _transitionFrom = null;
            _transitionFromIndex = -1;
        }

        internal void SyncSelectionOffset(int selectedIndex)
        {
            if (!UsesViewportFractionLayout())
            {
                SetOffset(WithPrimaryOffset(_offset, selectedIndex));
                return;
            }

            var currentOffset = GetPrimaryOffset(_offset);
            var targetOffset = GetNearestLogicalOffset(selectedIndex, currentOffset);

            if (MathUtilities.AreClose(currentOffset, targetOffset))
            {
                SetOffset(WithPrimaryOffset(_offset, targetOffset));
                return;
            }

            if (_isDragging)
                return;

            var transition = GetTransition();
            var canAnimate = transition is not null && Math.Abs(targetOffset - currentOffset) <= 1.001;

            if (!canAnimate)
            {
                ResetViewportTransitionState();
                ClearFractionalProgressContext();
                SetOffset(WithPrimaryOffset(_offset, targetOffset));
                return;
            }

            var fromIndex = Items.Count > 0 ? NormalizeIndex((int)Math.Round(currentOffset), Items.Count) : -1;
            var forward = targetOffset > currentOffset;

            ResetViewportTransitionState();
            SetFractionalProgressContext(fromIndex, selectedIndex, forward, currentOffset, targetOffset);
            _ = AnimateViewportOffsetAsync(
                currentOffset,
                targetOffset,
                TimeSpan.FromSeconds(MaxCompletionDuration),
                new QuadraticEaseOut(),
                () =>
                {
                    ResetViewportTransitionState();
                    ClearFractionalProgressContext();

                    // SyncScrollOffset is blocked during animation and the post-animation layout
                    // still sees a live CTS, so re-sync explicitly in case SelectedIndex changed.
                    if (ItemsControl is Carousel carousel)
                        SyncSelectionOffset(carousel.SelectedIndex);
                });
        }

        /// <summary>
        /// Refreshes the gesture recognizer based on the carousel's IsSwipeEnabled and PageTransition settings.
        /// </summary>
        internal void RefreshGestureRecognizer()
        {
            TeardownGestureRecognizer();

            if (ItemsControl is not Carousel carousel || !carousel.IsSwipeEnabled)
                return;

            _swipeAxis = UsesViewportFractionLayout() ? carousel.GetLayoutAxis() : carousel.GetTransitionAxis();

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
            _completionCts = null;
            CancelOffsetAnimation();

            if (_swipeGestureRecognizer is not null)
            {
                GestureRecognizers.Remove(_swipeGestureRecognizer);
                _swipeGestureRecognizer = null;
            }

            RemoveHandler(InputElement.SwipeGestureEvent, OnSwipeGesture);
            RemoveHandler(InputElement.SwipeGestureEndedEvent, OnSwipeGestureEnded);
            ResetSwipeState();
        }

        private Control? FindViewportControl(int itemIndex)
        {
            return _viewportRealized.Values.FirstOrDefault(x => x.ItemIndex == itemIndex)?.Control;
        }

        private void SetFractionalProgressContext(int fromIndex, int toIndex, bool forward, double startOffset, double targetOffset)
        {
            _progressFromIndex = fromIndex;
            _progressToIndex = toIndex;
            _isForward = forward;
            _progressStartOffset = startOffset;
            _activeViewportTargetOffset = targetOffset;
        }

        private void ClearFractionalProgressContext()
        {
            _progressFromIndex = -1;
            _progressToIndex = -1;
            _progressStartOffset = 0;
            _activeViewportTargetOffset = 0;
        }

        private double GetFractionalTransitionProgress(double currentOffset)
        {
            var totalDistance = Math.Abs(_activeViewportTargetOffset - _progressStartOffset);
            if (totalDistance <= 0)
                return 0;

            return Math.Clamp(Math.Abs(currentOffset - _progressStartOffset) / totalDistance, 0, 1);
        }

        private void ResetViewportTransitionState()
        {
            foreach (var element in _viewportRealized.Values.Select(x => x.Control))
                ResetTransitionState(element);
        }

        private void OnSwipeGesture(object? sender, SwipeGestureEventArgs e)
        {
            if (ItemsControl is not Carousel carousel || !carousel.IsSwipeEnabled)
                return;

            if (UsesViewportFractionLayout())
            {
                OnViewportFractionSwipeGesture(carousel, e);
                return;
            }

            if (_realizedIndex < 0 || Items.Count == 0)
                return;

            if (_completionCts is { IsCancellationRequested: false })
            {
                _completionCts.Cancel();
                _completionCts = null;

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
                // Lock the axis on gesture start to keep diagonal drags stable.
                _lockedAxis = _swipeAxis ?? (Math.Abs(e.Delta.X) >= Math.Abs(e.Delta.Y) ?
                    PageSlide.SlideAxis.Horizontal :
                    PageSlide.SlideAxis.Vertical);
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

            // Clamp so totalDelta cannot cross zero (absorbs touch jitter).
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

            if (GetTransition() is IProgressPageTransition progressive)
            {
                progressive.Update(
                    progress,
                    _realized,
                    _isRubberBanding ? null : _swipeTarget,
                    _isForward,
                    size,
                    Array.Empty<PageTransitionItem>());
            }

            e.Handled = true;
        }

        private void OnViewportFractionSwipeGesture(Carousel carousel, SwipeGestureEventArgs e)
        {
            if (_offsetAnimationCts is { IsCancellationRequested: false })
            {
                CancelOffsetAnimation();
                SetOffset(WithPrimaryOffset(_offset, GetNearestLogicalOffset(carousel.SelectedIndex, GetPrimaryOffset(_offset))));
            }

            if (_isDragging && e.Id != _swipeGestureId)
                return;

            var delta = _lockedAxis == PageSlide.SlideAxis.Horizontal ? e.Delta.X : e.Delta.Y;

            if (!_isDragging)
            {
                _lockedAxis = carousel.GetLayoutAxis();
                _swipeGestureId = e.Id;
                _dragStartOffset = GetNearestLogicalOffset(carousel.SelectedIndex, GetPrimaryOffset(_offset));
                _totalDelta = 0;
                _isDragging = true;
                _isRubberBanding = false;
                carousel.IsSwiping = true;
                _isForward = delta > 0;
                var targetIndex = _isForward ? carousel.SelectedIndex + 1 : carousel.SelectedIndex - 1;

                if (targetIndex >= Items.Count || targetIndex < 0)
                {
                    if (carousel.WrapSelection && Items.Count > 1)
                        targetIndex = NormalizeIndex(targetIndex, Items.Count);
                    else
                        _isRubberBanding = true;
                }

                var targetOffset = _isForward ? _dragStartOffset + 1 : _dragStartOffset - 1;
                SetFractionalProgressContext(
                    carousel.SelectedIndex,
                    _isRubberBanding ? -1 : targetIndex,
                    _isForward,
                    _dragStartOffset,
                    targetOffset);
                ResetViewportTransitionState();
            }

            _totalDelta += delta;

            if (_isForward)
                _totalDelta = Math.Max(0, _totalDelta);
            else
                _totalDelta = Math.Min(0, _totalDelta);

            var itemExtent = GetViewportItemExtent(Bounds.Size);
            if (itemExtent <= 0)
                return;

            var logicalDelta = Math.Clamp(Math.Abs(_totalDelta) / itemExtent, 0, 1);
            var proposedOffset = _dragStartOffset + (_isForward ? logicalDelta : -logicalDelta);

            if (!_isRubberBanding)
            {
                proposedOffset = Math.Clamp(
                    proposedOffset,
                    Math.Min(_dragStartOffset, _activeViewportTargetOffset),
                    Math.Max(_dragStartOffset, _activeViewportTargetOffset));
            }
            else if (proposedOffset < 0)
            {
                proposedOffset = -(RubberBandFactor * Math.Sqrt(-proposedOffset));
            }
            else
            {
                var maxOffset = Math.Max(0, Items.Count - 1);
                proposedOffset = maxOffset + (RubberBandFactor * Math.Sqrt(proposedOffset - maxOffset));
            }

            SetOffset(WithPrimaryOffset(_offset, proposedOffset));

            if (GetTransition() is IProgressPageTransition progressive)
            {
                var currentOffset = GetPrimaryOffset(_offset);
                var progress = Math.Clamp(Math.Abs(currentOffset - _dragStartOffset), 0, 1);
                progressive.Update(
                    progress,
                    FindViewportControl(_progressFromIndex),
                    FindViewportControl(_progressToIndex),
                    _isForward,
                    GetViewportItemExtent(Bounds.Size),
                    BuildFractionalVisibleItems(currentOffset));
            }

            e.Handled = true;
        }

        private void OnViewportFractionSwipeGestureEnded(Carousel carousel, SwipeGestureEndedEventArgs e)
        {
            var itemExtent = GetViewportItemExtent(Bounds.Size);
            var currentOffset = GetPrimaryOffset(_offset);
            var currentProgress = Math.Abs(currentOffset - _dragStartOffset);
            var velocity = _lockedAxis == PageSlide.SlideAxis.Horizontal ? Math.Abs(e.Velocity.X) : Math.Abs(e.Velocity.Y);
            var targetIndex = _progressToIndex;
            var canCommit = !_isRubberBanding && targetIndex >= 0;
            var commit = canCommit &&
                         (currentProgress >= SwipeCommitThreshold ||
                          (velocity > VelocityCommitThreshold && currentProgress >= MinSwipeDistanceForVelocityCommit));
            var endOffset = commit
                ? _activeViewportTargetOffset
                : GetNearestLogicalOffset(carousel.SelectedIndex, currentOffset);
            var remainingDistance = Math.Abs(endOffset - currentOffset);
            var durationSeconds = _isRubberBanding
                ? RubberBandReturnDuration
                : velocity > 0 && itemExtent > 0
                    ? Math.Clamp(remainingDistance * itemExtent / velocity, MinCompletionDuration, MaxCompletionDuration)
                    : MaxCompletionDuration;
            var easing = _isRubberBanding ? (Easing)new SineEaseOut() : new QuadraticEaseOut();

            _isDragging = false;
            _ = AnimateViewportOffsetAsync(
                currentOffset,
                endOffset,
                TimeSpan.FromSeconds(durationSeconds),
                easing,
                () =>
                {
                    _totalDelta = 0;
                    _isRubberBanding = false;
                    carousel.IsSwiping = false;

                    if (commit)
                    {
                        SetOffset(WithPrimaryOffset(_offset, GetNearestLogicalOffset(targetIndex, endOffset)));
                        carousel.SelectedIndex = targetIndex;
                    }
                    else
                    {
                        SetOffset(WithPrimaryOffset(_offset, GetNearestLogicalOffset(carousel.SelectedIndex, endOffset)));
                    }

                    ResetViewportTransitionState();
                    ClearFractionalProgressContext();
                });
        }

        private async Task AnimateViewportOffsetAsync(
            double fromOffset,
            double toOffset,
            TimeSpan duration,
            Easing easing,
            Action onCompleted)
        {
            CancelOffsetAnimation();
            var offsetAnimationCts = new CancellationTokenSource();
            _offsetAnimationCts = offsetAnimationCts;
            var cancellationToken = offsetAnimationCts.Token;

            var animation = new Animation.Animation
            {
                FillMode = FillMode.Forward,
                Duration = duration,
                Easing = easing,
                Children =
                {
                    new KeyFrame
                    {
                        Setters = { new Setter(OffsetAnimationProgressProperty, 0d) },
                        Cue = new Cue(0d)
                    },
                    new KeyFrame
                    {
                        Setters = { new Setter(OffsetAnimationProgressProperty, 1d) },
                        Cue = new Cue(1d)
                    }
                }
            };

            _offsetAnimationStart = fromOffset;
            _offsetAnimationTarget = toOffset;
            SetValue(OffsetAnimationProgressProperty, 0d);

            try
            {
                await animation.RunAsync(this, null, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    return;

                SetOffset(WithPrimaryOffset(_offset, toOffset));

                if (UsesViewportFractionLayout() &&
                    GetTransition() is IProgressPageTransition progressive)
                {
                    var transitionProgress = GetFractionalTransitionProgress(toOffset);
                    progressive.Update(
                        transitionProgress,
                        FindViewportControl(_progressFromIndex),
                        FindViewportControl(_progressToIndex),
                        _isForward,
                        GetViewportItemExtent(Bounds.Size),
                        BuildFractionalVisibleItems(toOffset));
                }

                onCompleted();
            }
            finally
            {
                if (ReferenceEquals(_offsetAnimationCts, offsetAnimationCts))
                    _offsetAnimationCts = null;
            }
        }

        private void OnSwipeGestureEnded(object? sender, SwipeGestureEndedEventArgs e)
        {
            if (!_isDragging || e.Id != _swipeGestureId || ItemsControl is not Carousel carousel)
                return;

            if (UsesViewportFractionLayout())
            {
                OnViewportFractionSwipeGestureEnded(carousel, e);
                return;
            }

            var size = _lockedAxis == PageSlide.SlideAxis.Horizontal ? Bounds.Width : Bounds.Height;
            var rawProgress = size > 0 ? Math.Abs(_totalDelta) / size : 0;
            var currentProgress = _isRubberBanding
                ? RubberBandFactor * Math.Sqrt(rawProgress)
                : rawProgress;
            var velocity = _lockedAxis == PageSlide.SlideAxis.Horizontal
                ? Math.Abs(e.Velocity.X)
                : Math.Abs(e.Velocity.Y);
            var commit = !_isRubberBanding
                         && (currentProgress >= SwipeCommitThreshold ||
                             (velocity > VelocityCommitThreshold && currentProgress >= MinSwipeDistanceForVelocityCommit))
                         && _swipeTarget is not null;

            _completionEndProgress = commit ? 1.0 : 0.0;
            var remainingDistance = Math.Abs(_completionEndProgress - currentProgress);
            var durationSeconds = _isRubberBanding
                ? RubberBandReturnDuration
                : velocity > 0
                ? Math.Clamp(remainingDistance * size / velocity, MinCompletionDuration, MaxCompletionDuration)
                : MaxCompletionDuration;
            Easing easing = _isRubberBanding ? new SineEaseOut() : new QuadraticEaseOut();

            _completionCts?.Cancel();
            var completionCts = new CancellationTokenSource();
            _completionCts = completionCts;

            SetValue(CompletionProgressProperty, currentProgress);

            var animation = new Animation.Animation
            {
                FillMode = FillMode.Forward,
                Easing = easing,
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
            _ = RunCompletionAnimation(animation, carousel, completionCts);
        }

        private async Task RunCompletionAnimation(
            Animation.Animation animation,
            Carousel carousel,
            CancellationTokenSource completionCts)
        {
            var cancellationToken = completionCts.Token;

            try
            {
                await animation.RunAsync(this, null, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    return;

                if (GetTransition() is IProgressPageTransition progressive)
                {
                    var swipeTarget = ReferenceEquals(_realized, _swipeTarget) ? null : _swipeTarget;
                    var size = _lockedAxis == PageSlide.SlideAxis.Horizontal ? Bounds.Width : Bounds.Height;
                    progressive.Update(
                        _completionEndProgress,
                        _realized,
                        swipeTarget,
                        _isForward,
                        size,
                        Array.Empty<PageTransitionItem>());
                }

                var commit = _completionEndProgress > 0.5;

                if (commit && _swipeTarget is not null)
                {
                    var targetIndex = _swipeTargetIndex;
                    var targetElement = _swipeTarget;

                    // Clear swipe target state before promoting it to the realized element so
                    // interactive transitions never receive the same control as both from/to.
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
            finally
            {
                if (ReferenceEquals(_completionCts, completionCts))
                    _completionCts = null;
            }
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == OffsetAnimationProgressProperty)
            {
                if (_offsetAnimationCts is { IsCancellationRequested: false })
                {
                    var animProgress = change.GetNewValue<double>();
                    var primaryOffset = _offsetAnimationStart +
                        ((_offsetAnimationTarget - _offsetAnimationStart) * animProgress);
                    SetOffset(WithPrimaryOffset(_offset, primaryOffset));

                    if (UsesViewportFractionLayout() &&
                        GetTransition() is IProgressPageTransition progressive)
                    {
                        var transitionProgress = GetFractionalTransitionProgress(primaryOffset);
                        progressive.Update(
                            transitionProgress,
                            FindViewportControl(_progressFromIndex),
                            FindViewportControl(_progressToIndex),
                            _isForward,
                            GetViewportItemExtent(Bounds.Size),
                            BuildFractionalVisibleItems(primaryOffset));
                    }
                }
            }
            else if (change.Property == CompletionProgressProperty)
            {
                var isCompletionAnimating = _completionCts is { IsCancellationRequested: false };

                if (!_isDragging && _swipeTarget is null && !isCompletionAnimating)
                    return;

                var progress = change.GetNewValue<double>();
                if (GetTransition() is IProgressPageTransition progressive)
                {
                    var swipeTarget = ReferenceEquals(_realized, _swipeTarget) ? null : _swipeTarget;
                    var size = _lockedAxis == PageSlide.SlideAxis.Horizontal ? Bounds.Width : Bounds.Height;
                    progressive.Update(
                        progress,
                        _realized,
                        swipeTarget,
                        _isForward,
                        size,
                        Array.Empty<PageTransitionItem>());
                }
            }
        }

        private IReadOnlyList<PageTransitionItem> BuildFractionalVisibleItems(double currentOffset)
        {
            var items = new PageTransitionItem[_viewportRealized.Count];
            var i = 0;
            foreach (var entry in _viewportRealized.OrderBy(x => x.Key))
            {
                items[i++] = new PageTransitionItem(
                    entry.Value.ItemIndex,
                    entry.Value.Control,
                    entry.Key - currentOffset);
            }

            return items;
        }

        private void ResetSwipeState()
        {
            if (ItemsControl is Carousel carousel)
                carousel.IsSwiping = false;

            CancelOffsetAnimation();

            ResetViewportTransitionState();
            ResetTransitionState(_realized);

            if (_swipeTarget is not null)
                RecycleElement(_swipeTarget);

            _isDragging = false;
            _totalDelta = 0;
            _swipeTarget = null;
            _swipeTargetIndex = -1;
            _isRubberBanding = false;
            ClearFractionalProgressContext();

            if (UsesViewportFractionLayout() && ItemsControl is Carousel viewportCarousel)
                SetOffset(WithPrimaryOffset(_offset, GetNearestLogicalOffset(viewportCarousel.SelectedIndex, GetPrimaryOffset(_offset))));
        }

        private void ResetTransitionState(Control? control)
        {
            if (control is null)
                return;

            if (GetTransition() is IProgressPageTransition progressive)
            {
                progressive.Reset(control);
            }
            else
            {
                ResetVisualState(control);
            }
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
