using System;
using System.Collections.Specialized;

namespace Avalonia.Controls.Repeaters
{
    public class StackLayout : VirtualizingLayout, IFlowLayoutAlgorithmDelegates
    {
        public static readonly AvaloniaProperty<Orientation> OrientationProperty
            = StackPanel.OrientationProperty.AddOwner<StackLayout>();

        public static readonly AvaloniaProperty<double> SpacingProperty
            = StackPanel.SpacingProperty.AddOwner<StackLayout>();

        private readonly OrientationBasedMeasures _orientation = new OrientationBasedMeasures();

        public Orientation Orientation
        {
            get => GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        public double Spacing
        {
            get => GetValue(SpacingProperty);
            set => SetValue(SpacingProperty, value);
        }

        public Rect GetExtent(
            Size availableSize,
            VirtualizingLayoutContext context,
            IControl firstRealized,
            int firstRealizedItemIndex,
            Rect firstRealizedLayoutBounds,
            IControl lastRealized,
            int lastRealizedItemIndex,
            Rect lastRealizedLayoutBounds)
        {
            var extent = new Rect();

            // Constants
            int itemsCount = context.ItemCount;
            var stackState = (StackLayoutState)context.LayoutState;
            double averageElementSize = GetAverageElementSize(availableSize, context, stackState) + Spacing;

            _orientation.SetMinorSize(ref extent, stackState.MaxArrangeBounds);
            _orientation.SetMajorSize(ref extent, Math.Max(0.0f, itemsCount * averageElementSize - Spacing));
            if (itemsCount > 0)
            {
                if (firstRealized != null)
                {
                    //MUX_ASSERT(lastRealized);
                    _orientation.SetMajorStart(
                        ref extent,
                        _orientation.MajorStart(firstRealizedLayoutBounds) - firstRealizedItemIndex * averageElementSize);
                    var remainingItems = itemsCount - lastRealizedItemIndex - 1;
                    _orientation.SetMajorSize(
                        ref extent,
                        _orientation.MajorEnd(lastRealizedLayoutBounds) -
                            _orientation.MajorStart(extent) + 
                            (remainingItems * averageElementSize));
                }
            }

            return extent;
        }

        public void OnElementMeasured(
            IControl element,
            int index,
            Size availableSize,
            Size measureSize,
            Size desiredSize,
            Size provisionalArrangeSize,
            VirtualizingLayoutContext context)
        {
            if (context is VirtualizingLayoutContext virtualContext)
            {
                var stackState = (StackLayoutState)virtualContext.LayoutState;
                var provisionalArrangeSizeWinRt = provisionalArrangeSize;
                stackState.OnElementMeasured(
                    index,
                    _orientation.Major(provisionalArrangeSizeWinRt),
                    _orientation.Minor(provisionalArrangeSizeWinRt));
            }
        }

        Size IFlowLayoutAlgorithmDelegates.Algorithm_GetMeasureSize(
            int index,
            Size availableSize,
            VirtualizingLayoutContext context) => availableSize;

        Size IFlowLayoutAlgorithmDelegates.Algorithm_GetProvisionalArrangeSize(
            int index,
            Size measureSize,
            Size desiredSize,
            VirtualizingLayoutContext context)
        {
            var measureSizeMinor = _orientation.Minor(measureSize);
            return _orientation.MinorMajorSize(
                double.IsInfinity(measureSizeMinor) ?
                    Math.Max(measureSizeMinor, _orientation.Minor(desiredSize)) :
                    _orientation.Minor(desiredSize),
                _orientation.Major(desiredSize));
        }

        bool IFlowLayoutAlgorithmDelegates.Algorithm_ShouldBreakLine(int index, double remainingSpace) => true;

        FlowLayoutAnchorInfo IFlowLayoutAlgorithmDelegates.Algorithm_GetAnchorForRealizationRect(
            Size availableSize,
            VirtualizingLayoutContext context) => GetAnchorForRealizationRect(availableSize, context);

        FlowLayoutAnchorInfo IFlowLayoutAlgorithmDelegates.Algorithm_GetAnchorForTargetElement(
            int targetIndex,
            Size availableSize,
            VirtualizingLayoutContext context)
        {
            double offset = double.NaN;
            int index = -1;
            int itemsCount = context.ItemCount;

            if (targetIndex >= 0 && targetIndex < itemsCount)
            {
                index = targetIndex;
                var state = (StackLayoutState)context.LayoutState;
                double averageElementSize = GetAverageElementSize(availableSize, context, state) + Spacing;
                offset = index * averageElementSize + _orientation.MajorStart(state.FlowAlgorithm.LastExtent);
            }

            return new FlowLayoutAnchorInfo { Index = index, Offset = offset };
        }

        Rect IFlowLayoutAlgorithmDelegates.Algorithm_GetExtent(
            Size availableSize,
            VirtualizingLayoutContext context,
            IControl firstRealized,
            int firstRealizedItemIndex,
            Rect firstRealizedLayoutBounds,
            IControl lastRealized,
            int lastRealizedItemIndex,
            Rect lastRealizedLayoutBounds)
        {
            return GetExtent(
                availableSize,
                context,
                firstRealized,
                firstRealizedItemIndex,
                firstRealizedLayoutBounds,
                lastRealized,
                lastRealizedItemIndex,
                lastRealizedLayoutBounds);
        }

        void IFlowLayoutAlgorithmDelegates.Algorithm_OnElementMeasured(IControl element, int index, Size availableSize, Size measureSize, Size desiredSize, Size provisionalArrangeSize, VirtualizingLayoutContext context)
        {
            OnElementMeasured(
                element,
                index,
                availableSize,
                measureSize,
                desiredSize,
                provisionalArrangeSize,
                context);
        }

        void IFlowLayoutAlgorithmDelegates.Algorithm_OnLineArranged(int startIndex, int countInLine, double lineSize, VirtualizingLayoutContext context)
        {
        }

        internal FlowLayoutAnchorInfo GetAnchorForRealizationRect(
            Size availableSize,
            VirtualizingLayoutContext context)
        {
            int anchorIndex = -1;
            double offset = double.NaN;

            // Constants
            int itemsCount = context.ItemCount;
            if (itemsCount > 0)
            {
                var realizationRect = context.RealizationRect;
                var state = (StackLayoutState)context.LayoutState;
                var lastExtent = state.FlowAlgorithm.LastExtent;

                double averageElementSize = GetAverageElementSize(availableSize, context, state) + Spacing;
                double realizationWindowOffsetInExtent = _orientation.MajorStart(realizationRect) - _orientation.MajorStart(lastExtent);
                double majorSize = _orientation.MajorSize(lastExtent) == 0 ? Math.Max(0.0, averageElementSize * itemsCount - Spacing) : _orientation.MajorSize(lastExtent);
                if (itemsCount > 0 &&
                    _orientation.MajorSize(realizationRect) > 0 &&
                    realizationWindowOffsetInExtent + _orientation.MajorSize(realizationRect) >= 0 && realizationWindowOffsetInExtent <= majorSize)
                {
                    anchorIndex = (int) (realizationWindowOffsetInExtent / averageElementSize);
                    offset = anchorIndex* averageElementSize + _orientation.MajorStart(lastExtent);
                    anchorIndex = Math.Max(0, Math.Min(itemsCount - 1, anchorIndex));
                }
        }

            return new FlowLayoutAnchorInfo { Index = anchorIndex, Offset = offset, };
        }

        protected override void InitializeForContextCore(VirtualizingLayoutContext context)
        {
            var state = context.LayoutState;
            var stackState = state as StackLayoutState;
            
            if (stackState == null)
            {
                if (state != null)
                {
                    throw new InvalidOperationException("LayoutState must derive from StackLayoutState.");
                }

                // Custom deriving layouts could potentially be stateful.
                // If that is the case, we will just create the base state required by UniformGridLayout ourselves.
                stackState = new StackLayoutState();
            }

            stackState.InitializeForContext(context, this);
        }

        protected override void UninitializeForContextCore(VirtualizingLayoutContext context)
        {
            var stackState = (StackLayoutState)context.LayoutState;
            stackState.UninitializeForContext(context);
        }

        protected override Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
        {
            var desiredSize = GetFlowAlgorithm(context).Measure(
                availableSize,
                context,
                false,
                0,
                Spacing,
                _orientation.ScrollOrientation,
                LayoutId);

            return new Size(desiredSize.Width, desiredSize.Height);
        }

        protected override Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
        {
            var value = GetFlowAlgorithm(context).Arrange(
               finalSize,
               context,
               FlowLayoutAlgorithm.LineAlignment.Start,
               LayoutId);

            ((StackLayoutState)context.LayoutState).OnArrangeLayoutEnd();

            return new Size(value.Width, value.Height);
        }

        protected internal override void OnItemsChangedCore(VirtualizingLayoutContext context, object source, NotifyCollectionChangedEventArgs args)
        {
            GetFlowAlgorithm(context).OnItemsSourceChanged(source, args, context);
            // Always invalidate layout to keep the view accurate.
            InvalidateLayout();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == OrientationProperty)
            {
                //Note: For StackLayout Vertical Orientation means we have a Vertical ScrollOrientation.
                //Horizontal Orientation means we have a Horizontal ScrollOrientation.
                _orientation.ScrollOrientation = (Orientation)e.NewValue;
            }

            InvalidateLayout();
        }

        private double GetAverageElementSize(
            Size availableSize,
            VirtualizingLayoutContext context,
            StackLayoutState stackLayoutState)
        {
            double averageElementSize = 0;

            if (context.ItemCount > 0)
            {
                if (stackLayoutState.TotalElementsMeasured == 0)
                {
                    var tmpElement = context.GetOrCreateElementAt(0, ElementRealizationOptions.ForceCreate | ElementRealizationOptions.SuppressAutoRecycle);
                    stackLayoutState.FlowAlgorithm.MeasureElement(tmpElement, 0, availableSize, context);
                    context.RecycleElement(tmpElement);
                }

                averageElementSize = Math.Round(stackLayoutState.TotalElementSize / stackLayoutState.TotalElementsMeasured);
            }

            return averageElementSize;
        }

        private void InvalidateLayout() => InvalidateMeasure();

        private FlowLayoutAlgorithm GetFlowAlgorithm(VirtualizingLayoutContext context) => ((StackLayoutState)context.LayoutState).FlowAlgorithm;
    }
}
