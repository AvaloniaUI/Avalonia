namespace Perspex.Controls
{
    using System;
    using System.Diagnostics.Contracts;
    using Perspex.Layout;
    using Perspex.Media;

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

    public abstract class Control : Visual, ILayoutable
    {
        public static readonly PerspexProperty<Brush> BackgroundProperty =
            PerspexProperty.Register<Border, Brush>("Background");

        public static readonly PerspexProperty<Brush> BorderBrushProperty =
            PerspexProperty.Register<Border, Brush>("BorderBrush");

        public static readonly PerspexProperty<double> BorderThicknessProperty =
            PerspexProperty.Register<Border, double>("BorderThickness");

        public static readonly PerspexProperty<HorizontalAlignment> HorizontalAlignmentProperty =
            PerspexProperty.Register<Control, HorizontalAlignment>("HorizontalAlignment");

        public static readonly PerspexProperty<VerticalAlignment> VerticalAlignmentProperty =
            PerspexProperty.Register<Control, VerticalAlignment>("VerticalAlignment");

        public static readonly PerspexProperty<Thickness> MarginProperty =
            PerspexProperty.Register<Control, Thickness>("Margin");

        public Brush Background
        {
            get { return this.GetValue(BackgroundProperty); }
            set { this.SetValue(BackgroundProperty, value); }
        }

        public Brush BorderBrush
        {
            get { return this.GetValue(BorderBrushProperty); }
            set { this.SetValue(BorderBrushProperty, value); }
        }

        public double BorderThickness
        {
            get { return this.GetValue(BorderThicknessProperty); }
            set { this.SetValue(BorderThicknessProperty, value); }
        }

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

        public Control Parent
        {
            get;
            internal set;
        }

        public ILayoutRoot GetLayoutRoot()
        {
            Control c = this;

            while (c != null && !(c is ILayoutRoot))
            {
                c = c.Parent;
            }

            return (ILayoutRoot)c;
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

        public void InvalidateArrange()
        {
            this.GetLayoutRoot().LayoutManager.InvalidateArrange(this);
        }

        public void InvalidateMeasure()
        {
            this.GetLayoutRoot().LayoutManager.InvalidateMeasure(this);
        }

        protected virtual Size ArrangeContent(Size finalSize)
        {
            return finalSize;
        }

        protected abstract Size MeasureContent(Size availableSize);
    }
}
