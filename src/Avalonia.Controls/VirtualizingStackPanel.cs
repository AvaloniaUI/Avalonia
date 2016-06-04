// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Specialized;
using Avalonia.Layout;

namespace Avalonia.Controls
{
    public class VirtualizingStackPanel : StackPanel, IVirtualizingPanel
    {
        private double _takenSpace;
        private int _canBeRemoved;
        private double _averageItemSize;
        private int _averageCount;
        private double _pixelOffset;

        bool IVirtualizingPanel.IsFull
        {
            get
            {
                return Orientation == Orientation.Horizontal ?
                    _takenSpace >= AvailableSpace.Width :
                    _takenSpace >= AvailableSpace.Height;
            }
        }

        int IVirtualizingPanel.OverflowCount => _canBeRemoved;

        Orientation IVirtualizingPanel.ScrollDirection => Orientation;

        double IVirtualizingPanel.AverageItemSize => _averageItemSize;

        double IVirtualizingPanel.PixelOverflow
        {
            get
            {
                var bounds = Orientation == Orientation.Horizontal ? 
                    Bounds.Width : Bounds.Height;
                return Math.Max(0, (_takenSpace - _pixelOffset) - bounds);
            }
        }

        double IVirtualizingPanel.PixelOffset
        {
            get { return _pixelOffset; }

            set
            {
                if (_pixelOffset != value)
                {
                    _pixelOffset = value;
                    InvalidateArrange();
                }
            }
        }

        // TODO: We need to put a reasonable limit on this, probably based on the max window size.
        private Size AvailableSpace => ((ILayoutable)this).PreviousMeasure ?? Bounds.Size;

        protected override Size ArrangeOverride(Size finalSize)
        {
            _canBeRemoved = 0;
            _takenSpace = 0;
            _averageItemSize = 0;
            _averageCount = 0;
            var result = base.ArrangeOverride(finalSize);
            return result;
        }

        protected override void ChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            base.ChildrenChanged(sender, e);

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (IControl control in e.NewItems)
                    {
                        UpdateAdd(control);
                    }

                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (IControl control in e.OldItems)
                    {
                        UpdateRemove(control);
                    }

                    break;
            }
        }

        internal override void ArrangeChild(
            IControl child, 
            Rect rect,
            Size panelSize,
            Orientation orientation)
        {
            if (orientation == Orientation.Vertical)
            {
                rect = new Rect(rect.X, rect.Y - _pixelOffset, rect.Width, rect.Height);
                child.Arrange(rect);

                if (rect.Y >= AvailableSpace.Height)
                {
                    ++_canBeRemoved;
                }

                if (rect.Bottom >= _takenSpace)
                {
                    _takenSpace = rect.Bottom;
                }

                AddToAverageItemSize(rect.Height);
            }
            else
            {
                rect = new Rect(rect.X - _pixelOffset, rect.Y, rect.Width, rect.Height);
                child.Arrange(rect);

                if (rect.X >= AvailableSpace.Width)
                {
                    ++_canBeRemoved;
                }

                if (rect.Right >= _takenSpace)
                {
                    _takenSpace = rect.Right;
                }

                AddToAverageItemSize(rect.Width);
            }
        }

        private void UpdateAdd(IControl child)
        {
            var bounds = Bounds;
            var gap = Gap;

            child.Measure(AvailableSpace);
            ++_averageCount;

            if (Orientation == Orientation.Vertical)
            {
                var height = child.DesiredSize.Height;
                _takenSpace += height + gap;
                AddToAverageItemSize(height);
            }
            else
            {
                var width = child.DesiredSize.Width;
                _takenSpace += width + gap;
                AddToAverageItemSize(width);
            }
        }

        private void UpdateRemove(IControl child)
        {
            var bounds = Bounds;
            var gap = Gap;

            if (Orientation == Orientation.Vertical)
            {
                var height = child.DesiredSize.Height;
                _takenSpace -= height + gap;
                RemoveFromAverageItemSize(height);
            }
            else
            {
                var width = child.DesiredSize.Width;
                _takenSpace -= width + gap;
                RemoveFromAverageItemSize(width);
            }
        }

        private void AddToAverageItemSize(double value)
        {
            ++_averageCount;
            _averageItemSize += (value - _averageItemSize) / _averageCount;
        }

        private void RemoveFromAverageItemSize(double value)
        {
            _averageItemSize = ((_averageItemSize * _averageCount) - value) / (_averageCount - 1);
            --_averageCount;
        }
    }
}
