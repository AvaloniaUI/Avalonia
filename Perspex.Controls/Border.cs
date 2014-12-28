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
        public static readonly PerspexProperty<Brush> BackgroundProperty =
            PerspexProperty.Register<Border, Brush>("Background");

        public static readonly PerspexProperty<Brush> BorderBrushProperty =
            PerspexProperty.Register<Border, Brush>("BorderBrush");

        public static readonly PerspexProperty<double> BorderThicknessProperty =
            PerspexProperty.Register<Border, double>("BorderThickness");

        static Border()
        {
            Control.AffectsRender(Border.BackgroundProperty);
            Control.AffectsRender(Border.BorderBrushProperty);
        }

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
            var content = this.Content;
            var padding = this.Padding + new Thickness(this.BorderThickness);

            if (content != null)
            {
                content.Measure(availableSize.Deflate(padding));
                return content.DesiredSize.Value.Inflate(padding);
            }

            return new Size();
        }
    }
}
