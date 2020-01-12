﻿using System;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Metadata;

namespace Avalonia.Controls
{
    [Obsolete("Use Image control with DrawingImage source")]
    public class DrawingPresenter : Control
    {
        static DrawingPresenter()
        {
            AffectsMeasure<DrawingPresenter>(DrawingProperty);
            AffectsRender<DrawingPresenter>(DrawingProperty);
        }

        public static readonly StyledProperty<Drawing> DrawingProperty =
            AvaloniaProperty.Register<DrawingPresenter, Drawing>(nameof(Drawing));

        public static readonly StyledProperty<Stretch> StretchProperty =
            AvaloniaProperty.Register<DrawingPresenter, Stretch>(nameof(Stretch), Stretch.Uniform);

        [Content]
        public Drawing Drawing
        {
            get => GetValue(DrawingProperty);
            set => SetValue(DrawingProperty, value);
        }

        public Stretch Stretch
        {
            get => GetValue(StretchProperty);
            set => SetValue(StretchProperty, value);
        }

        private Matrix _transform = Matrix.Identity;

        protected override Size MeasureOverride(Size availableSize)
        {
            if (Drawing == null) return new Size();

            var (size, transform) = Shape.CalculateSizeAndTransform(availableSize, Drawing.GetBounds(), Stretch);

            _transform = transform;

            return size;
        }

        public override void Render(DrawingContext context)
        {
            if (Drawing != null)
            {
                using (context.PushPreTransform(_transform))
                using (context.PushClip(new Rect(Bounds.Size)))
                {
                    Drawing.Draw(context);
                }
            }
        }
    }
}
