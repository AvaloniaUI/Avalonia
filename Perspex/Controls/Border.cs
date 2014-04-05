// -----------------------------------------------------------------------
// <copyright file="Border.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Reactive.Linq;
    using Perspex.Media;

    public class Border : Decorator
    {
        public Border()
        {
            // Hacky hack hack!
            Observable.Merge(
                this.GetObservable(BackgroundProperty),
                this.GetObservable(BorderBrushProperty))
                .Subscribe(_ => this.InvalidateArrange());
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
