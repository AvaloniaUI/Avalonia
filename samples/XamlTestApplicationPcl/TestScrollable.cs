using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace XamlTestApplication
{
    public class TestScrollable : Control, ILogicalScrollable
    {
        private int itemCount = 100;
        private Size _extent;
        private Vector _offset;
        private Size _viewport;
        private Size _lineSize;

        public bool IsLogicalScrollEnabled => true;
        public Action InvalidateScroll { get; set; }

        Size IScrollable.Extent
        {
            get { return _extent; }
        }

        Vector IScrollable.Offset
        {
            get { return _offset; }

            set
            {
                _offset = value;
                InvalidateVisual();
            }
        }

        Size IScrollable.Viewport
        {
            get { return _viewport; }
        }

        public Size ScrollSize
        {
            get
            {
                return new Size(double.PositiveInfinity, 1);
            }
        }

        public Size PageScrollSize
        {
            get
            {
                return new Size(double.PositiveInfinity, Bounds.Height);
            }
        }

        public override void Render(DrawingContext context)
        {
            var y = 0.0;

            for (var i = (int)_offset.Y; i < itemCount; ++i)
            {
                var line = new FormattedText(
                    "Item " + (i + 1),
                    TextBlock.GetFontFamily(this),
                    TextBlock.GetFontSize(this),
                    Size.Infinity,
                    TextBlock.GetFontStyle(this),
                    TextAlignment.Left,
                    TextBlock.GetFontWeight(this));
                context.DrawText(Brushes.Black, new Point(-_offset.X, y), line);
                y += _lineSize.Height;
            }
        }

        public bool BringIntoView(IControl target, Rect targetRect)
        {
            throw new NotImplementedException();
        }

        public IControl GetControlInDirection(NavigationDirection direction, IControl from)
        {
            throw new NotImplementedException();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var line = new FormattedText(
                "Item 100",
                TextBlock.GetFontFamily(this),
                TextBlock.GetFontSize(this),
                Size.Infinity,
                TextBlock.GetFontStyle(this),
                TextAlignment.Left,
                TextBlock.GetFontWeight(this));
            line.Constraint = availableSize;
            _lineSize = line.Measure();
            return new Size(_lineSize.Width, _lineSize.Height * itemCount);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _viewport = new Size(finalSize.Width, finalSize.Height / _lineSize.Height);
            _extent = new Size(_lineSize.Width, itemCount + 1);
            InvalidateScroll?.Invoke();
            return finalSize;
        }
    }
}