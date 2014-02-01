namespace Perspex.Controls
{
    using System;
    using System.Reactive.Linq;
    using Perspex.Media;

    public class Border : Decorator
    {
        public override void Render(IDrawingContext context)
        {
            Brush background = this.Background;
            Brush borderBrush = this.BorderBrush;
            double borderThickness = this.BorderThickness;

            if (background != null)
            {
                System.Diagnostics.Debug.WriteLine(
                    string.Format("{0} Draw Background={1}",
                        this.GetHashCode(),
                        background));
                context.FillRectange(background, new Rect(this.Bounds.Size));
            }

            if (borderBrush != null && borderThickness > 0)
            {
                context.DrawRectange(new Pen(borderBrush, borderThickness), new Rect(this.Bounds.Size));
            }
        }
    }
}
