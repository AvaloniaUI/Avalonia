using System;
using Perspex;
using Perspex.Controls;
using Perspex.Controls.Primitives;
using Perspex.Media;

namespace XamlTestApplication
{
    public class TestScrollable : Control, IScrollable
    {
        private int itemCount = 100;
        private Size _extent;
        private Vector _offset;
        private Size _viewport;
        private Size _lineSize;

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

        protected override Size MeasureOverride(Size availableSize)
        {
            using (var line = new FormattedText(
                "Item 100",
                TextBlock.GetFontFamily(this),
                TextBlock.GetFontSize(this),
                TextBlock.GetFontStyle(this),
                TextAlignment.Left,
                TextBlock.GetFontWeight(this)))
            {
                line.Constraint = availableSize;
                _lineSize = line.Measure();
                return new Size(_lineSize.Width, _lineSize.Height * itemCount);
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _viewport = new Size(finalSize.Width, finalSize.Height / _lineSize.Height);
            _extent = new Size(_lineSize.Width, itemCount + 1);
            InvalidateScroll?.Invoke();
            return finalSize;
        }

        public override void Render(DrawingContext context)
        {
            var y = 0.0;

            for (var i = (int)_offset.Y; i < itemCount; ++i)
            {
                using (var line = new FormattedText(
                    "Item " + (i + 1),
                    TextBlock.GetFontFamily(this),
                    TextBlock.GetFontSize(this),
                    TextBlock.GetFontStyle(this),
                    TextAlignment.Left,
                    TextBlock.GetFontWeight(this)))
                {
                    context.DrawText(Brushes.Black, new Point(-_offset.X, y), line);
                    y += _lineSize.Height;
                }
            }
        }
    }
}