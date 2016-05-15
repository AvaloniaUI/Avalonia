// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Specialized;

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
                    _takenSpace >= Bounds.Width :
                    _takenSpace >= Bounds.Height;
            }
        }

        int IVirtualizingPanel.OverflowCount => _canBeRemoved;

        double IVirtualizingPanel.AverageItemSize => _averageItemSize;

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

        Action IVirtualizingPanel.ArrangeCompleted { get; set; }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _canBeRemoved = 0;
            _takenSpace = 0;
            _averageItemSize = 0;
            _averageCount = 0;
            var result = base.ArrangeOverride(finalSize);
            ((IVirtualizingPanel)this).ArrangeCompleted?.Invoke();
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

                if (rect.Y >= panelSize.Height)
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

                if (rect.X >= panelSize.Width)
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

            child.Measure(bounds.Size);
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
