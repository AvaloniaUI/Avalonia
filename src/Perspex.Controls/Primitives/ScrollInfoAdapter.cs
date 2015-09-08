// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Controls.Primitives
{
    public class ScrollInfoAdapter : IScrollInfo
    {
        private readonly IScrollInfoBase _nfo;
        public ScrollInfoAdapter(IScrollInfoBase nfo)
        {
            _nfo = nfo;
        }

        public ScrollViewer ScrollOwner
        {
            get { return _nfo.ScrollOwner; }
            set { _nfo.ScrollOwner = value; }
        }

        public double ExtentWidth => (_nfo as IHorizontalScrollInfo)?.ExtentWidth ?? 0;

        public double ViewportWidth => (_nfo as IHorizontalScrollInfo)?.ViewportWidth ?? 0;

        public double ExtentHeight => (_nfo as IVerticalScrollInfo)?.ExtentHeight ?? 0;

        public double ViewportHeight => (_nfo as IVerticalScrollInfo)?.ViewportHeight ?? 0;

        private double _horizontalOffset;
        public double HorizontalOffset
        {
            get
            {
                return (_nfo as IHorizontalScrollInfo)?.HorizontalOffset ?? _horizontalOffset;
            }

            set
            {
                var info = (_nfo as IHorizontalScrollInfo);
                if (info == null)
                    _horizontalOffset = value;
                else
                    info.HorizontalOffset = value;
            }
        }

        private double _verticalOffset;
        public double VerticalOffset
        {
            get
            {
                return (_nfo as IVerticalScrollInfo)?.VerticalOffset ?? _verticalOffset;
            }

            set
            {
                var info = (_nfo as IVerticalScrollInfo);
                if (info == null)
                    _verticalOffset = value;
                else
                    info.VerticalOffset = value;
            }
        }

        public void LineLeft() => (_nfo as IHorizontalScrollInfo)?.LineLeft();

        public void LineRight() => (_nfo as IHorizontalScrollInfo)?.LineRight();

        public void MouseWheelLeft() => (_nfo as IHorizontalScrollInfo)?.MouseWheelLeft();

        public void MouseWheelRight() => (_nfo as IHorizontalScrollInfo)?.MouseWheelRight();

        public void PageLeft() => (_nfo as IHorizontalScrollInfo)?.PageLeft();

        public Rect MakeVisible(Visual visual, Rect rectangle) => _nfo.MakeVisible(visual, rectangle);

        public void PageRight() => (_nfo as IHorizontalScrollInfo)?.PageRight();

        public void LineDown() => (_nfo as IVerticalScrollInfo)?.LineDown();

        public void LineUp() => (_nfo as IVerticalScrollInfo)?.LineUp();

        public void MouseWheelDown() => (_nfo as IVerticalScrollInfo)?.MouseWheelDown();

        public void MouseWheelUp() => (_nfo as IVerticalScrollInfo)?.MouseWheelUp();

        public void PageDown() => (_nfo as IVerticalScrollInfo)?.PageDown();

        public void PageUp() => (_nfo as IVerticalScrollInfo)?.PageUp();

        private bool _canVerticallyScroll;
        public bool CanVerticallyScroll
        {
            get
            {
                return (_nfo as IVerticalScrollInfo)?.CanVerticallyScroll ?? _canVerticallyScroll;
            }

            set
            {
                var info = (_nfo as IVerticalScrollInfo);
                if (info == null)
                    _canVerticallyScroll = value;
                else
                    info.CanVerticallyScroll = value;
            }
        }

        private bool _canHorizontallyScroll;
        public bool CanHorizontallyScroll
        {
            get
            {
                return (_nfo as IHorizontalScrollInfo)?.CanHorizontallyScroll ?? _canHorizontallyScroll;
            }

            set
            {
                var info = (_nfo as IHorizontalScrollInfo);
                if (info == null)
                    _canHorizontallyScroll = value;
                else
                    info.CanHorizontallyScroll = value;
            }
        }
    }
}
