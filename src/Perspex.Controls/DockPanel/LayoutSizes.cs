namespace Perspex.Controls
{
    using System;

    public struct LayoutSizes
    {
        private readonly Size _size;
        private readonly Size _maxSize;
        private readonly Size _minSize;

        public LayoutSizes(Size size, Size maxSize, Size minSize)
        {
            _size = size;
            _maxSize = maxSize;
            _minSize = minSize;
        }

        public Size MinSize => _minSize;

        public Size MaxSize => _maxSize;

        public Size Size => _size;

        public bool IsWidthSpecified => !double.IsNaN(_size.Width);
        public bool IsHeightSpecified => !double.IsNaN(_size.Height);
    }
}