using System;
using Avalonia.Media;

namespace Avalonia.Controls
{
    /// <summary>
    /// Viewbox is used to scale single child.
    /// </summary>
    /// <seealso cref="Avalonia.Controls.Decorator" />
    public class Viewbox : Decorator
    {
        /// <summary>
        /// The stretch property
        /// </summary>
        public static readonly AvaloniaProperty<Stretch> StretchProperty =
                AvaloniaProperty.RegisterDirect<Viewbox, Stretch>(nameof(Stretch),
                    v => v.Stretch, (c, v) => c.Stretch = v, Stretch.Uniform);

        private Stretch _stretch = Stretch.Uniform;

        /// <summary>
        /// Gets or sets the stretch mode, 
        /// which determines how child fits into the available space.
        /// </summary>
        /// <value>
        /// The stretch.
        /// </value>
        public Stretch Stretch
        {
            get => _stretch;
            set => SetAndRaise(StretchProperty, ref _stretch, value);
        }

        static Viewbox()
        {
            ClipToBoundsProperty.OverrideDefaultValue<Viewbox>(true);
            AffectsMeasure<Viewbox>(StretchProperty);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var child = Child;

            if (child != null)
            {
                child.Measure(Size.Infinity);

                var childSize = child.DesiredSize;

                var scale = GetScale(availableSize, childSize, Stretch);

                return (childSize * scale).Constrain(availableSize);
            }

            return new Size();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var child = Child;

            if (child != null)
            {
                var childSize = child.DesiredSize;
                var scale = GetScale(finalSize, childSize, Stretch);
                var scaleTransform = child.RenderTransform as ScaleTransform;

                if (scaleTransform == null)
                {
                    child.RenderTransform = scaleTransform = new ScaleTransform(scale.X, scale.Y);
                    child.RenderTransformOrigin = RelativePoint.TopLeft;
                }

                scaleTransform.ScaleX = scale.X;
                scaleTransform.ScaleY = scale.Y;

                child.Arrange(new Rect(childSize));

                return childSize * scale;
            }

            return new Size();
        }

        private static Vector GetScale(Size availableSize, Size childSize, Stretch stretch)
        {
            double scaleX = 1.0;
            double scaleY = 1.0;

            bool validWidth = !double.IsPositiveInfinity(availableSize.Width);
            bool validHeight = !double.IsPositiveInfinity(availableSize.Height);

            if (stretch != Stretch.None && (validWidth || validHeight))
            {
                scaleX = childSize.Width <= 0.0 ? 0.0 : availableSize.Width / childSize.Width;
                scaleY = childSize.Height <= 0.0 ? 0.0 : availableSize.Height / childSize.Height;

                if (!validWidth)
                {
                    scaleX = scaleY;
                }
                else if (!validHeight)
                {
                    scaleY = scaleX;
                }
                else
                {
                    switch (stretch)
                    {
                        case Stretch.Uniform:
                            scaleX = scaleY = Math.Min(scaleX, scaleY);
                            break;

                        case Stretch.UniformToFill:
                            scaleX = scaleY = Math.Max(scaleX, scaleY);
                            break;
                    }
                }
            }

            return new Vector(scaleX, scaleY);
        }
    }
}
