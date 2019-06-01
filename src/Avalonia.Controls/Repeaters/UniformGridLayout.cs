using System;
using System.Collections.Specialized;

namespace Avalonia.Controls.Repeaters
{
    public enum UniformGridLayoutItemsJustification
    {
        Start = 0,
        Center = 1,
        End = 2,
        SpaceAround = 3,
        SpaceBetween = 4,
        SpaceEvenly = 5,
    };

    public enum UniformGridLayoutItemsStretch
    {
        None = 0,
        Fill = 1,
        Uniform = 2,
    };

    public class UniformGridLayout : VirtualizingLayout, IFlowLayoutAlgorithmDelegates
    {
        public static readonly StyledProperty<UniformGridLayoutItemsJustification> ItemsJustificationProperty =
            AvaloniaProperty.Register<UniformGridLayout, UniformGridLayoutItemsJustification>(nameof(ItemsJustification));

        public static readonly StyledProperty<UniformGridLayoutItemsStretch> ItemsStretchProperty =
            AvaloniaProperty.Register<UniformGridLayout, UniformGridLayoutItemsStretch>(nameof(ItemsStretch));

        public static readonly StyledProperty<double> MinColumnSpacingProperty =
            AvaloniaProperty.Register<UniformGridLayout, double>(nameof(MinColumnSpacing));

        public static readonly StyledProperty<double> MinItemHeightProperty =
            AvaloniaProperty.Register<UniformGridLayout, double>(nameof(MinItemHeight));

        public static readonly StyledProperty<double> MinItemWidthProperty =
            AvaloniaProperty.Register<UniformGridLayout, double>(nameof(MinItemWidth));

        public static readonly StyledProperty<double> MinRowSpacingProperty =
            AvaloniaProperty.Register<UniformGridLayout, double>(nameof(MinRowSpacing));

        public static readonly StyledProperty<Orientation> OrientationProperty =
            StackPanel.OrientationProperty.AddOwner<StackLayout>();

        private readonly OrientationBasedMeasures _orientation = new OrientationBasedMeasures();
        private double _minItemWidth = double.NaN;
        private double _minItemHeight = double.NaN;
        private double _minRowSpacing;
        private double _minColumnSpacing;
        private UniformGridLayoutItemsJustification _itemsJustification;
        private UniformGridLayoutItemsStretch _itemsStretch;

        public UniformGridLayout()
        {
            LayoutId = "UniformGridLayout";
        } 

        static UniformGridLayout()
        {
            OrientationProperty.OverrideDefaultValue<UniformGridLayout>(Orientation.Horizontal);
        }

        public UniformGridLayoutItemsJustification ItemsJustification
        {
            get => GetValue(ItemsJustificationProperty);
            set => SetValue(ItemsJustificationProperty, value);
        }

        public UniformGridLayoutItemsStretch ItemsStretch
        {
            get => GetValue(ItemsStretchProperty);
            set => SetValue(ItemsStretchProperty, value);
        }

        public double MinColumnSpacing
        {
            get => GetValue(MinColumnSpacingProperty);
            set => SetValue(MinColumnSpacingProperty, value);
        }

        public double MinItemHeight
        {
            get => GetValue(MinItemHeightProperty);
            set => SetValue(MinItemHeightProperty, value);
        }

        public double MinItemWidth
        {
            get => GetValue(MinItemWidthProperty);
            set => SetValue(MinItemWidthProperty, value);
        }

        public double MinRowSpacing
        {
            get => GetValue(MinRowSpacingProperty);
            set => SetValue(MinRowSpacingProperty, value);
        }

        public Orientation Orientation
        {
            get => GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        internal double LineSpacing => Orientation == Orientation.Horizontal ? _minRowSpacing : _minColumnSpacing;
        internal double MinItemSpacing => Orientation == Orientation.Horizontal ? _minColumnSpacing : _minRowSpacing;

        Size IFlowLayoutAlgorithmDelegates.Algorithm_GetMeasureSize(
            int index,
            Size availableSize,
            VirtualizingLayoutContext context)
        {
            var gridState = (UniformGridLayoutState)context.LayoutState;
            return new Size(gridState.EffectiveItemWidth, gridState.EffectiveItemHeight);
        }

        Size IFlowLayoutAlgorithmDelegates.Algorithm_GetProvisionalArrangeSize(
            int index,
            Size measureSize,
            Size desiredSize,
            VirtualizingLayoutContext context)
        {
            var gridState = (UniformGridLayoutState)context.LayoutState;
            return new Size(gridState.EffectiveItemWidth, gridState.EffectiveItemHeight);
        }

        bool IFlowLayoutAlgorithmDelegates.Algorithm_ShouldBreakLine(int index, double remainingSpace) => remainingSpace < 0;

        FlowLayoutAnchorInfo IFlowLayoutAlgorithmDelegates.Algorithm_GetAnchorForRealizationRect(
            Size availableSize,
            VirtualizingLayoutContext context)
        {
            Rect bounds = new Rect(double.NaN, double.NaN, double.NaN, double.NaN);
            int anchorIndex = -1;

            int itemsCount = context.ItemCount;
            var realizationRect = context.RealizationRect;
            if (itemsCount > 0 && _orientation.MajorSize(realizationRect) > 0)
            {
                var gridState = (UniformGridLayoutState)context.LayoutState;
                var lastExtent = gridState.FlowAlgorithm.LastExtent;
                int itemsPerLine = Math.Max(1, (int)(_orientation.Minor(availableSize) / GetMinorSizeWithSpacing(context)));
                double majorSize = (itemsCount / itemsPerLine) * GetMajorSizeWithSpacing(context);
                double realizationWindowStartWithinExtent = _orientation.MajorStart(realizationRect) - _orientation.MajorStart(lastExtent);
                if ((realizationWindowStartWithinExtent + _orientation.MajorSize(realizationRect)) >= 0 && realizationWindowStartWithinExtent <= majorSize)
                {
                    double offset = Math.Max(0.0, _orientation.MajorStart(realizationRect) - _orientation.MajorStart(lastExtent));
                    int anchorRowIndex = (int)(offset / GetMajorSizeWithSpacing(context));

                    anchorIndex = Math.Max(0, Math.Min(itemsCount - 1, anchorRowIndex * itemsPerLine));
                    bounds = GetLayoutRectForDataIndex(availableSize, anchorIndex, lastExtent, context);
                }
            }

            return new FlowLayoutAnchorInfo
            {
                Index = anchorIndex,
                Offset = _orientation.MajorStart(bounds)
            };
        }

        FlowLayoutAnchorInfo IFlowLayoutAlgorithmDelegates.Algorithm_GetAnchorForTargetElement(
            int targetIndex,
            Size availableSize,
            VirtualizingLayoutContext context)
        {
            int index = -1;
            double offset = double.NaN;
            int count = context.ItemCount;
            if (targetIndex >= 0 && targetIndex < count)
            {
                int itemsPerLine = Math.Max(1, (int)(_orientation.Minor(availableSize) / GetMinorSizeWithSpacing(context)));
                int indexOfFirstInLine = (targetIndex / itemsPerLine) * itemsPerLine;
                index = indexOfFirstInLine;
                var state = context.LayoutState as UniformGridLayoutState;
                offset = _orientation.MajorStart(GetLayoutRectForDataIndex(availableSize, indexOfFirstInLine, state.FlowAlgorithm.LastExtent, context));
            }

            return new FlowLayoutAnchorInfo
            {
                Index = index,
                Offset = offset
            };
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
            var extent = new Rect();


            // Constants
            int itemsCount = context.ItemCount;
            double availableSizeMinor = _orientation.Minor(availableSize);
            int itemsPerLine = Math.Max(1, !double.IsInfinity(availableSizeMinor) ?
                (int)(availableSizeMinor / GetMinorSizeWithSpacing(context)) : itemsCount);
            double lineSize = GetMajorSizeWithSpacing(context);

            if (itemsCount > 0)
            {
                _orientation.SetMinorSize(
                    ref extent,
                    !double.IsInfinity(availableSizeMinor) ?
                    availableSizeMinor :
                    Math.Max(0.0, itemsCount * GetMinorSizeWithSpacing(context) - (double)MinItemSpacing));
                _orientation.SetMajorSize(
                    ref extent,
                    Math.Max(0.0, (itemsCount / itemsPerLine) * lineSize - (double)LineSpacing));

                if (firstRealized != null)
                {
                    ////MUX_ASSERT(lastRealized);

                    _orientation.SetMajorStart(
                        ref extent,
                        _orientation.MajorStart(firstRealizedLayoutBounds) - (firstRealizedItemIndex / itemsPerLine) * lineSize);
                    int remainingItems = itemsCount - lastRealizedItemIndex - 1;
                    _orientation.SetMajorSize(
                        ref extent,
                        _orientation.MajorEnd(lastRealizedLayoutBounds) - _orientation.MajorStart(extent) + (remainingItems / itemsPerLine) * lineSize);
                }
            }

            return extent;
        }

        void IFlowLayoutAlgorithmDelegates.Algorithm_OnElementMeasured(IControl element, int index, Size availableSize, Size measureSize, Size desiredSize, Size provisionalArrangeSize, VirtualizingLayoutContext context)
        {
        }

        void IFlowLayoutAlgorithmDelegates.Algorithm_OnLineArranged(int startIndex, int countInLine, double lineSize, VirtualizingLayoutContext context)
        {
        }

        protected override void InitializeForContextCore(VirtualizingLayoutContext context)
        {
            var state = context.LayoutState;
            var gridState = state as UniformGridLayoutState;
            
            if (gridState == null)
            {
                if (state != null)
                {
                    throw new InvalidOperationException("LayoutState must derive from UniformGridLayoutState.");
                }

                // Custom deriving layouts could potentially be stateful.
                // If that is the case, we will just create the base state required by UniformGridLayout ourselves.
                gridState = new UniformGridLayoutState();
            }

            gridState.InitializeForContext(context, this);
        }

        protected override void UninitializeForContextCore(VirtualizingLayoutContext context)
        {
            var gridState = (UniformGridLayoutState)context.LayoutState;
            gridState.UninitializeForContext(context);
        }

        protected override Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
        {
            // Set the width and height on the grid state. If the user already set them then use the preset. 
            // If not, we have to measure the first element and get back a size which we're going to be using for the rest of the items.
            var gridState = (UniformGridLayoutState)context.LayoutState;
            gridState.EnsureElementSize(availableSize, context, _minItemWidth, _minItemHeight, _itemsStretch, Orientation, MinRowSpacing, MinColumnSpacing);

            var desiredSize = GetFlowAlgorithm(context).Measure(
                availableSize,
                context,
                true,
                MinItemSpacing,
                LineSpacing,
                _orientation.ScrollOrientation,
                LayoutId);

            return new Size(desiredSize.Width, desiredSize.Height);
        }

        protected override Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
        {
            var value = GetFlowAlgorithm(context).Arrange(
               finalSize,
               context,
               (FlowLayoutAlgorithm.LineAlignment)_itemsJustification,
               LayoutId);
            return new Size(value.Width, value.Height);
        }

        protected internal override void OnItemsChangedCore(VirtualizingLayoutContext context, object source, NotifyCollectionChangedEventArgs args)
        {
            GetFlowAlgorithm(context).OnItemsSourceChanged(source, args, context);
            // Always invalidate layout to keep the view accurate.
            InvalidateLayout();

            var gridState = (UniformGridLayoutState)context.LayoutState;
            gridState.ClearElementOnDataSourceChange(context, args);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs args)
        {
            if (args.Property == OrientationProperty)
            {
                var orientation = (Orientation)args.NewValue;

                //Note: For UniformGridLayout Vertical Orientation means we have a Horizontal ScrollOrientation. Horizontal Orientation means we have a Vertical ScrollOrientation.
                //i.e. the properties are the inverse of each other.
                var scrollOrientation = (orientation == Orientation.Horizontal) ? Orientation.Vertical : Orientation.Horizontal;
                _orientation.ScrollOrientation = scrollOrientation;
            }
            else if (args.Property == MinColumnSpacingProperty)
            {
                _minColumnSpacing = (double)args.NewValue;
            }
            else if (args.Property == MinRowSpacingProperty)
            {
                _minRowSpacing = (double)args.NewValue;
            }
            else if (args.Property == ItemsJustificationProperty)
            {
                _itemsJustification = (UniformGridLayoutItemsJustification)args.NewValue;
            }
            else if (args.Property == ItemsStretchProperty)
            {
                _itemsStretch = (UniformGridLayoutItemsStretch)args.NewValue;
            }
            else if (args.Property == MinItemWidthProperty)
            {
                _minItemWidth = (double)args.NewValue;
            }
            else if (args.Property == MinItemHeightProperty)
            {
                _minItemHeight = (double)args.NewValue;
            }

            InvalidateLayout();
        }

        private double GetMinorSizeWithSpacing(VirtualizingLayoutContext context)
        {
            var minItemSpacing = MinItemSpacing;
            var gridState = (UniformGridLayoutState)context.LayoutState;
            return _orientation.ScrollOrientation == Orientation.Vertical?
                gridState.EffectiveItemWidth + minItemSpacing :
                gridState.EffectiveItemHeight + minItemSpacing;
        }

        private double GetMajorSizeWithSpacing(VirtualizingLayoutContext context)
        {
            var lineSpacing = LineSpacing;
            var gridState = (UniformGridLayoutState)context.LayoutState;
            return _orientation.ScrollOrientation == Orientation.Vertical ?
                gridState.EffectiveItemHeight + lineSpacing :
                gridState.EffectiveItemWidth + lineSpacing;
        }

        Rect GetLayoutRectForDataIndex(
            Size availableSize,
            int index,
            Rect lastExtent,
            VirtualizingLayoutContext context)
        {
            int itemsPerLine = Math.Max(1, (int)(_orientation.Minor(availableSize) / GetMinorSizeWithSpacing(context)));
            int rowIndex = (int)(index / itemsPerLine);
            int indexInRow = index - (rowIndex * itemsPerLine);

            var gridState = (UniformGridLayoutState)context.LayoutState;
            Rect bounds = _orientation.MinorMajorRect(
                indexInRow * GetMinorSizeWithSpacing(context) + _orientation.MinorStart(lastExtent),
                rowIndex * GetMajorSizeWithSpacing(context) + _orientation.MajorStart(lastExtent),
                _orientation.ScrollOrientation == Orientation.Vertical ? gridState.EffectiveItemWidth : gridState.EffectiveItemHeight,
                _orientation.ScrollOrientation == Orientation.Vertical ? gridState.EffectiveItemHeight : gridState.EffectiveItemWidth);

            return bounds;
        }

        private void InvalidateLayout() => InvalidateMeasure();

        private FlowLayoutAlgorithm GetFlowAlgorithm(VirtualizingLayoutContext context) => ((UniformGridLayoutState)context.LayoutState).FlowAlgorithm;
    }
}
