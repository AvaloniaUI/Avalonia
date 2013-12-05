namespace Perspex.Controls
{
    using System.Diagnostics.Contracts;

    public enum HorizontalAlignment
    {
        Stretch,
        Left,
        Center,
        Right,
    }

    public enum VerticalAlignment
    {
        Stretch,
        Top,
        Center,
        Bottom,
    }

    public abstract class Control : Visual
    {
        public static readonly PerspexProperty<HorizontalAlignment> HorizontalAlignmentProperty =
            PerspexProperty.Register<Control, HorizontalAlignment>("HorizontalAlignment");

        public static readonly PerspexProperty<VerticalAlignment> VerticalAlignmentProperty =
            PerspexProperty.Register<Control, VerticalAlignment>("VerticalAlignment");

        public static readonly PerspexProperty<Thickness> MarginProperty =
            PerspexProperty.Register<Control, Thickness>("Margin");

        public Size? DesiredSize
        {
            get;
            set;
        }

        public HorizontalAlignment HorizontalAlignment
        {
            get { return this.GetValue(HorizontalAlignmentProperty); }
            set { this.SetValue(HorizontalAlignmentProperty, value); }
        }

        public VerticalAlignment VerticalAlignment
        {
            get { return this.GetValue(VerticalAlignmentProperty); }
            set { this.SetValue(VerticalAlignmentProperty, value); }
        }

        public Thickness Margin
        {
            get { return this.GetValue(MarginProperty); }
            set { this.SetValue(MarginProperty, value); }
        }

        public void Arrange(Rect rect)
        {
            this.Bounds = new Rect(
                rect.Position,
                this.ArrangeContent(rect.Size.Deflate(this.Margin).Constrain(rect.Size)));
        }

        public void Measure(Size availableSize)
        {
            availableSize = availableSize.Deflate(this.Margin);
            this.DesiredSize = this.MeasureContent(availableSize).Constrain(availableSize);
        }

        protected virtual Size ArrangeContent(Size finalSize)
        {
            return finalSize;
        }

        protected abstract Size MeasureContent(Size availableSize);
    }
}
