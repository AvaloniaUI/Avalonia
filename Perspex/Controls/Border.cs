namespace Perspex.Controls
{
    using System.Diagnostics.Contracts;
    using Perspex.Media;

    public class Border : Decorator
    {
        public static readonly PerspexProperty<Brush> BackgroundProperty =
            PerspexProperty.Register<Border, Brush>("Background");

        public Brush Background
        {
            get { return this.GetValue(BackgroundProperty); }
            set { this.SetValue(BackgroundProperty, value); }
        }

        public override void Render(IDrawingContext context)
        {
            Brush background = this.Background;

            if (background != null)
            {
                context.FillRectange(background, new Rect(this.Bounds.Size));
            }
        }
    }
}
