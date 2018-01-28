// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Specialized;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;

namespace Avalonia.Controls
{
    public class VirtualizingStackPanel : StackPanel, IVirtualizingPanel
    {
        private Size _availableSpace;
        private double _takenSpace;
        private int _canBeRemoved;
        private double _averageItemSize;
        private int _averageCount;
        private double _pixelOffset;
        private double _crossAxisOffset;
        private bool _forceRemeasure;

        bool IVirtualizingPanel.IsFull
        {
            get
            {
                return Orientation == Orientation.Horizontal ?
                    _takenSpace >= _availableSpace.Width :
                    _takenSpace >= _availableSpace.Height;
            }
        }

        IVirtualizingController IVirtualizingPanel.Controller { get; set; }
        int IVirtualizingPanel.OverflowCount => _canBeRemoved;
        Orientation IVirtualizingPanel.ScrollDirection => Orientation;
        double IVirtualizingPanel.AverageItemSize => _averageItemSize;

        double IVirtualizingPanel.PixelOverflow
        {
            get
            {
                var bounds = Orientation == Orientation.Horizontal ? 
                    _availableSpace.Width : _availableSpace.Height;
                return Math.Max(0, _takenSpace - bounds);
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

        double IVirtualizingPanel.CrossAxisOffset
        {
            get { return _crossAxisOffset; }

            set
            {
                if (_crossAxisOffset != value)
                {
                    _crossAxisOffset = value;
                    InvalidateArrange();
                }
            }
        }

        private IVirtualizingController Controller => ((IVirtualizingPanel)this).Controller;

        void IVirtualizingPanel.ForceInvalidateMeasure()
        {
            InvalidateMeasure();
            _forceRemeasure = true;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (_forceRemeasure || availableSize != ((ILayoutable)this).PreviousMeasure)
            {
                _forceRemeasure = false;
                _availableSpace = availableSize;
                Controller?.UpdateControls();
            }

            return base.MeasureOverride(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _availableSpace = finalSize;
            _canBeRemoved = 0;
            _takenSpace = 0;
            _averageItemSize = 0;
            _averageCount = 0;
            var result = base.ArrangeOverride(finalSize);
            _takenSpace += _pixelOffset;
            Controller?.UpdateControls();
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

        protected override IInputElement GetControlInDirection(NavigationDirection direction, IControl from)
        {
            if (from == null)
                return null;

            var logicalScrollable = Parent as ILogicalScrollable;

            if (logicalScrollable?.IsLogicalScrollEnabled == true)
            {
                return logicalScrollable.GetControlInDirection(direction, from);
            }
            else
            {
                return base.GetControlInDirection(direction, from);
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
                rect = new Rect(
                    rect.X - _crossAxisOffset,
                    rect.Y - _pixelOffset,
                    rect.Width,
                    rect.Height);
                child.Arrange(rect);

                if (rect.Y >= _availableSpace.Height)
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
                rect = new Rect(
                    rect.X - _pixelOffset,
                    rect.Y - _crossAxisOffset,
                    rect.Width,
                    rect.Height);
                child.Arrange(rect);

                if (rect.X >= _availableSpace.Width)
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

            child.Measure(_availableSpace);
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

            if (_canBeRemoved > 0)
            {
                --_canBeRemoved;
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
