namespace Perspex.Controls.Primitives
{
    public class ScrollInfoAdapter : IScrollInfo
    {
        private readonly IScrollInfoBase nfo;
        public ScrollInfoAdapter(IScrollInfoBase nfo)
        {
            this.nfo = nfo;
        }

        public ScrollViewer ScrollOwner
        {
            get { return this.nfo.ScrollOwner; }
            set { this.nfo.ScrollOwner = value; }
        }

        public double ExtentWidth => (this.nfo as IHorizontalScrollInfo)?.ExtentWidth ?? 0;

        public double ViewportWidth => (this.nfo as IHorizontalScrollInfo)?.ViewportWidth ?? 0;

        public double ExtentHeight => (this.nfo as IVerticalScrollInfo)?.ExtentHeight ?? 0;

        public double ViewportHeight => (this.nfo as IVerticalScrollInfo)?.ViewportHeight ?? 0;

        private double horizontalOffset;
        public double HorizontalOffset
        {
            get
            {
                return (this.nfo as IHorizontalScrollInfo)?.HorizontalOffset ?? this.horizontalOffset;
            }

            set
            {
                var info = (this.nfo as IHorizontalScrollInfo);
                if (info == null)
                    this.horizontalOffset = value;
                else
                    info.HorizontalOffset = value;
            }
        }

        private double verticalOffset;
        public double VerticalOffset
        {
            get
            {
                return (this.nfo as IVerticalScrollInfo)?.VerticalOffset ?? this.verticalOffset;
            }

            set
            {
                var info = (this.nfo as IVerticalScrollInfo);
                if (info == null)
                    this.verticalOffset = value;
                else
                    info.VerticalOffset = value;
            }
        }
        
        public void LineLeft() => (this.nfo as IHorizontalScrollInfo)?.LineLeft();

        public void LineRight() => (this.nfo as IHorizontalScrollInfo)?.LineRight();

        public void MouseWheelLeft() => (this.nfo as IHorizontalScrollInfo)?.MouseWheelLeft();

        public void MouseWheelRight() => (this.nfo as IHorizontalScrollInfo)?.MouseWheelRight();

        public void PageLeft() => (this.nfo as IHorizontalScrollInfo)?.PageLeft();

        public Rect MakeVisible(Visual visual, Rect rectangle) => this.nfo.MakeVisible(visual, rectangle);

        public void PageRight() => (this.nfo as IHorizontalScrollInfo)?.PageRight();

        public void LineDown() => (this.nfo as IVerticalScrollInfo)?.LineDown();

        public void LineUp() => (this.nfo as IVerticalScrollInfo)?.LineUp();

        public void MouseWheelDown() => (this.nfo as IVerticalScrollInfo)?.MouseWheelDown();

        public void MouseWheelUp() => (this.nfo as IVerticalScrollInfo)?.MouseWheelUp();

        public void PageDown() => (this.nfo as IVerticalScrollInfo)?.PageDown();

        public void PageUp() => (this.nfo as IVerticalScrollInfo)?.PageUp();

        private bool canVerticallyScroll;
        public bool CanVerticallyScroll
        {
            get
            {
                return (this.nfo as IVerticalScrollInfo)?.CanVerticallyScroll ?? this.canVerticallyScroll;
            }

            set
            {
                var info = (this.nfo as IVerticalScrollInfo);
                if (info == null)
                    this.canVerticallyScroll = value;
                else
                    info.CanVerticallyScroll = value;
            }
        }

        private bool canHorizontallyScroll;
        public bool CanHorizontallyScroll
        {
            get
            {
                return (this.nfo as IHorizontalScrollInfo)?.CanHorizontallyScroll ?? this.canHorizontallyScroll;
            }

            set
            {
                var info = (this.nfo as IHorizontalScrollInfo);
                if (info == null)
                    this.canHorizontallyScroll = value;
                else
                    info.CanHorizontallyScroll = value;
            }
        }

    }
}
