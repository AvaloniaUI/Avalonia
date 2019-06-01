using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace Avalonia.Controls.Repeaters
{
    public class UniformGridLayoutState
    {
        // We need to measure the element at index 0 to know what size to measure all other items. 
        // If FlowlayoutAlgorithm has already realized element 0 then we can use that. 
        // If it does not, then we need to do context.GetElement(0) at which point we have requested an element and are on point to clear it.
        // If we are responsible for clearing element 0 we keep m_cachedFirstElement valid. 
        // If we are not (because FlowLayoutAlgorithm is holding it for us) then we just null out this field and use the one from FlowLayoutAlgorithm.
        private IControl _cachedFirstElement;

        internal FlowLayoutAlgorithm FlowAlgorithm { get; } = new FlowLayoutAlgorithm();
        internal double EffectiveItemWidth { get; private set; }
        internal double EffectiveItemHeight { get; private set; }

        internal void InitializeForContext(VirtualizingLayoutContext context, IFlowLayoutAlgorithmDelegates callbacks)
        {
            FlowAlgorithm.InitializeForContext(context, callbacks);
            context.LayoutState = this;
        }

        internal void UninitializeForContext(VirtualizingLayoutContext context)
        {
            FlowAlgorithm.UninitializeForContext(context);

            if (_cachedFirstElement != null)
            {
                context.RecycleElement(_cachedFirstElement);
            }
        }

        internal void EnsureElementSize(
            Size availableSize,
            VirtualizingLayoutContext context,
            double layoutItemWidth,
            double LayoutItemHeight,
            UniformGridLayoutItemsStretch stretch,
            Orientation orientation,
            double minRowSpacing,
            double minColumnSpacing)
        {
            if (context.ItemCount > 0)
            {
                // If the first element is realized we don't need to cache it or to get it from the context
                var realizedElement = FlowAlgorithm.GetElementIfRealized(0);
                if (realizedElement != null)
                {
                    realizedElement.Measure(availableSize);
                    SetSize(realizedElement, layoutItemWidth, LayoutItemHeight, availableSize, stretch, orientation, minRowSpacing, minColumnSpacing);
                    _cachedFirstElement = null;
                }
                else
                {
                    if (_cachedFirstElement == null)
                    {
                        // we only cache if we aren't realizing it
                        _cachedFirstElement = context.GetOrCreateElementAt(
                            0,
                            ElementRealizationOptions.ForceCreate | ElementRealizationOptions.SuppressAutoRecycle); // expensive
                    }

                    _cachedFirstElement.Measure(availableSize);
                    SetSize(_cachedFirstElement, layoutItemWidth, LayoutItemHeight, availableSize, stretch, orientation, minRowSpacing, minColumnSpacing);

                    // See if we can move ownership to the flow algorithm. If we can, we do not need a local cache.
                    bool added = FlowAlgorithm.TryAddElement0(_cachedFirstElement);
                    if (added)
                    {
                        _cachedFirstElement = null;
                    }
                }
            }
        }

        private void SetSize(
            IControl element,
            double layoutItemWidth,
            double LayoutItemHeight,
            Size availableSize,
            UniformGridLayoutItemsStretch stretch,
            Orientation orientation,
            double minRowSpacing,
            double minColumnSpacing)
        {
            EffectiveItemWidth = (double.IsNaN(layoutItemWidth) ? element.DesiredSize.Width : layoutItemWidth);
            EffectiveItemHeight = (double.IsNaN(LayoutItemHeight) ? element.DesiredSize.Height : LayoutItemHeight);

            var availableSizeMinor = orientation == Orientation.Horizontal ? availableSize.Width : availableSize.Height;
            var minorItemSpacing = orientation == Orientation.Vertical ? minRowSpacing : minColumnSpacing;

            var itemSizeMinor = orientation == Orientation.Horizontal ? EffectiveItemWidth : EffectiveItemHeight;
            itemSizeMinor += minorItemSpacing;

            var numItemsPerColumn = (int)(Math.Max(1.0, availableSizeMinor / itemSizeMinor));
            var remainingSpace = ((int)availableSizeMinor) % ((int)itemSizeMinor);
            var extraMinorPixelsForEachItem = remainingSpace / numItemsPerColumn;

            if (stretch == UniformGridLayoutItemsStretch.Fill)
            {
                if (orientation == Orientation.Horizontal)
                {
                    EffectiveItemWidth += extraMinorPixelsForEachItem;
                }
                else
                {
                    EffectiveItemHeight += extraMinorPixelsForEachItem;
                }
            }
            else if (stretch == UniformGridLayoutItemsStretch.Uniform)
            {
                var itemSizeMajor = orientation == Orientation.Horizontal ? EffectiveItemHeight : EffectiveItemWidth;
                var extraMajorPixelsForEachItem = itemSizeMajor * (extraMinorPixelsForEachItem / itemSizeMinor);
                if (orientation == Orientation.Horizontal)
                {
                    EffectiveItemWidth += extraMinorPixelsForEachItem;
                    EffectiveItemHeight += extraMajorPixelsForEachItem;
                }
                else
                {
                    EffectiveItemHeight += extraMinorPixelsForEachItem;
                    EffectiveItemWidth += extraMajorPixelsForEachItem;
                }
            }
        }

        internal void EnsureFirstElementOwnership()
        {
            if (FlowAlgorithm.GetElementIfRealized(0) != null)
            {
                _cachedFirstElement = null;
            }
        }

        internal void ClearElementOnDataSourceChange(
            VirtualizingLayoutContext context,
            NotifyCollectionChangedEventArgs args)
        {
            if (_cachedFirstElement != null)
            {
                bool shouldClear = false;
                switch (args.Action)
                {
                case NotifyCollectionChangedAction.Add:
                    shouldClear = args.NewStartingIndex == 0;
                    break;

                case NotifyCollectionChangedAction.Replace:
                    shouldClear = args.NewStartingIndex == 0 || args.OldStartingIndex == 0;
                    break;

                case NotifyCollectionChangedAction.Remove:
                    shouldClear = args.OldStartingIndex == 0;
                    break;

                case NotifyCollectionChangedAction.Reset:
                    shouldClear = true;
                    break;

                case NotifyCollectionChangedAction.Move:
                    throw new NotImplementedException();
                }

                if (shouldClear)
                {
                    context.RecycleElement(_cachedFirstElement);
                    _cachedFirstElement = null;
                }
            }
        }
    }
}
