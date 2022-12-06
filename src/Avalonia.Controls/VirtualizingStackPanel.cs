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

        IVirtualizingController? IVirtualizingPanel.Controller { get; set; }
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

        private IVirtualizingController? Controller => ((IVirtualizingPanel)this).Controller;

        void IVirtualizingPanel.ForceInvalidateMeasure()
        {
            InvalidateMeasure();
            _forceRemeasure = true;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (_forceRemeasure || availableSize != PreviousMeasure)
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

        protected override void ChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            base.ChildrenChanged(sender, e);

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Control control in e.NewItems!)
                    {
                        UpdateAdd(control);
                    }

                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (Control control in e.OldItems!)
                    {
                        UpdateRemove(control);
                    }

                    break;
            }
        }

        protected override IInputElement? GetControlInDirection(NavigationDirection direction, Control? from)
        {
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
            Control child,
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

        private void UpdateAdd(Control child)
        {
            var bounds = Bounds;
            var spacing = Spacing;

            child.Measure(_availableSpace);
            ++_averageCount;

            if (Orientation == Orientation.Vertical)
            {
                var height = child.DesiredSize.Height;
                _takenSpace += height + spacing;
                AddToAverageItemSize(height);
            }
            else
            {
                var width = child.DesiredSize.Width;
                _takenSpace += width + spacing;
                AddToAverageItemSize(width);
            }
        }

        private void UpdateRemove(Control child)
        {
            var bounds = Bounds;
            var spacing = Spacing;

            if (Orientation == Orientation.Vertical)
            {
                var height = child.DesiredSize.Height;
                _takenSpace -= height + spacing;
                RemoveFromAverageItemSize(height);
            }
            else
            {
                var width = child.DesiredSize.Width;
                _takenSpace -= width + spacing;
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
