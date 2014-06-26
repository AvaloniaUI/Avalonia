// -----------------------------------------------------------------------
// <copyright file="Border.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Reactive.Linq;
    using Perspex.Layout;
    using Perspex.Media;

    public class Border : Decorator
    {
        public Border()
        {
            Observable.Merge(
                this.GetObservable(BackgroundProperty),
                this.GetObservable(BorderBrushProperty))
                .Subscribe(_ => this.InvalidateVisual());
        }

        public override void Render(IDrawingContext context)
        {
            Brush background = this.Background;
            Brush borderBrush = this.BorderBrush;
            double borderThickness = this.BorderThickness;
            Rect rect = new Rect(this.ActualSize).Deflate(BorderThickness / 2);

            if (background != null)
            {
                context.FillRectange(background, rect);
            }

            if (borderBrush != null && borderThickness > 0)
            {
                context.DrawRectange(new Pen(borderBrush, borderThickness), rect);
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Control content = this.Content;

            if (content != null)
            {
                Thickness padding = this.Padding + new Thickness(this.BorderThickness);
                content.Arrange(new Rect(finalSize).Deflate(padding));
            }

            return finalSize;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return LayoutHelper.MeasureDecorator(
                this, 
                this.Content, 
                availableSize, 
                this.Padding + new Thickness(this.BorderThickness));
        }
    }
}
