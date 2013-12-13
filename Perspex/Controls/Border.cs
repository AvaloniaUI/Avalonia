namespace Perspex.Controls
{
    using System.Diagnostics.Contracts;
    using Perspex.Media;

    public class Border : Decorator
    {
        public static readonly PerspexProperty<Brush> BackgroundProperty =
            PerspexProperty.Register<Border, Brush>("Background");

        public static readonly PerspexProperty<Brush> BorderBrushProperty =
            PerspexProperty.Register<Border, Brush>("BorderBrush");

        public static readonly PerspexProperty<double> BorderThicknessProperty =
            PerspexProperty.Register<Border, double>("BorderThickness");

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

        public override void Render(IDrawingContext context)
        {
            Brush background = this.Background;
            Brush borderBrush = this.BorderBrush;
            double borderThickness = this.BorderThickness;

            if (background != null)
            {
                context.FillRectange(background, new Rect(this.Bounds.Size));
            }

            if (borderBrush != null && borderThickness > 0)
            {
                context.DrawRectange(new Pen(borderBrush, borderThickness), new Rect(this.Bounds.Size));
            }
        }
    }
}
