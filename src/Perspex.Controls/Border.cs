// -----------------------------------------------------------------------
// <copyright file="Border.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using Perspex.Media;

    /// <summary>
    /// A control which decorates a child with a border and background.
    /// </summary>
    public class Border : Decorator
    {
        /// <summary>
        /// Defines the <see cref="Background"/> property.
        /// </summary>
        public static readonly PerspexProperty<Brush> BackgroundProperty =
            PerspexProperty.Register<Border, Brush>(nameof(Background));

        /// <summary>
        /// Defines the <see cref="BorderBrush"/> property.
        /// </summary>
        public static readonly PerspexProperty<Brush> BorderBrushProperty =
            PerspexProperty.Register<Border, Brush>(nameof(BorderBrush));

        /// <summary>
        /// Defines the <see cref="BorderThickness"/> property.
        /// </summary>
        public static readonly PerspexProperty<double> BorderThicknessProperty =
            PerspexProperty.Register<Border, double>(nameof(BorderThickness));

        /// <summary>
        /// Defines the <see cref="CornerRadius"/> property.
        /// </summary>
        public static readonly PerspexProperty<float> CornerRadiusProperty =
            PerspexProperty.Register<Border, float>(nameof(CornerRadius));

        /// <summary>
        /// Initializes static members of the <see cref="Border"/> class.
        /// </summary>
        static Border()
        {
            Control.AffectsRender(Border.BackgroundProperty);
            Control.AffectsRender(Border.BorderBrushProperty);
        }

        /// <summary>
        /// Gets or sets a brush with which to paint the background.
        /// </summary>
        public Brush Background
        {
            get { return this.GetValue(BackgroundProperty); }
            set { this.SetValue(BackgroundProperty, value); }
        }

        /// <summary>
        /// Gets or sets a brush with which to paint the border.
        /// </summary>
        public Brush BorderBrush
        {
            get { return this.GetValue(BorderBrushProperty); }
            set { this.SetValue(BorderBrushProperty, value); }
        }

        /// <summary>
        /// Gets or sets the thickness of the border.
        /// </summary>
        public double BorderThickness
        {
            get { return this.GetValue(BorderThicknessProperty); }
            set { this.SetValue(BorderThicknessProperty, value); }
        }

        /// <summary>
        /// Gets or sets the radius of the border rounded corners.
        /// </summary>
        public float CornerRadius
        {
            get { return this.GetValue(CornerRadiusProperty); }
            set { this.SetValue(CornerRadiusProperty, value); }
        }

        /// <summary>
        /// Renders the control.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        public override void Render(IDrawingContext context)
        {
            var background = this.Background;
            var borderBrush = this.BorderBrush;
            var borderThickness = this.BorderThickness;
            var cornerRadius = this.CornerRadius;
            var rect = new Rect(this.Bounds.Size).Deflate(this.BorderThickness);

            if (background != null)
            {
                context.FillRectange(background, rect, cornerRadius);
            }

            if (borderBrush != null && borderThickness > 0)
            {
                context.DrawRectange(new Pen(borderBrush, borderThickness), rect, cornerRadius);
            }
        }

        /// <summary>
        /// Measures the control.
        /// </summary>
        /// <param name="availableSize">The available size.</param>
        /// <returns>The desired size of the control.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            var child = this.Child;
            var padding = this.Padding + new Thickness(this.BorderThickness);

            if (child != null)
            {
                child.Measure(availableSize.Deflate(padding));
                return child.DesiredSize.Inflate(padding);
            }
            else
            {
                return new Size(padding.Left + padding.Right, padding.Bottom + padding.Top);
            }
        }

        /// <summary>
        /// Arranges the control's child.
        /// </summary>
        /// <param name="finalSize">The size allocated to the control.</param>
        /// <returns>The space taken.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var child = this.Child;

            if (child != null)
            {
                var padding = this.Padding + new Thickness(this.BorderThickness);
                child.Arrange(new Rect(finalSize).Deflate(padding));
            }

            return finalSize;
        }
    }
}