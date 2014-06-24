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
            if (this.Visibility == Visibility.Visible)
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
        }

        protected override Size ArrangeContent(Size finalSize)
        {
            return LayoutHelper.ArrangeDecorator(
                this,
                this.Content,
                finalSize,
                this.Padding + new Thickness(this.BorderThickness));
        }

        protected override Size MeasureContent(Size availableSize)
        {
            return LayoutHelper.MeasureDecorator(
                this, 
                this.Content, 
                availableSize, 
                this.Padding + new Thickness(this.BorderThickness));
        }
    }
}
