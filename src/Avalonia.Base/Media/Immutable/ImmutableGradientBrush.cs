using System.Collections.Generic;

namespace Avalonia.Media.Immutable
{
    /// <summary>
    /// A brush that draws with a gradient.
    /// </summary>
    public abstract class ImmutableGradientBrush : IGradientBrush, IImmutableBrush
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableGradientBrush"/> class.
        /// </summary>
        /// <param name="gradientStops">The gradient stops.</param>
        /// <param name="opacity">The opacity of the brush.</param>
        /// <param name="transform">The transform of the brush.</param>
        /// <param name="transformOrigin">The transform origin of the brush</param>
        /// <param name="spreadMethod">The spread method.</param>
        protected ImmutableGradientBrush(
            IReadOnlyList<ImmutableGradientStop> gradientStops,
            double opacity,
            ImmutableTransform? transform,
            RelativePoint? transformOrigin,
            GradientSpreadMethod spreadMethod)
        {
            GradientStops = gradientStops;
            Opacity = opacity;
            Transform = transform;
            TransformOrigin = transformOrigin.HasValue ? transformOrigin.Value : RelativePoint.TopLeft;
            SpreadMethod = spreadMethod;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableGradientBrush"/> class.
        /// </summary>
        /// <param name="source">The brush from which this brush's properties should be copied.</param>
        protected ImmutableGradientBrush(GradientBrush source)
            : this(source.GradientStops.ToImmutable(), source.Opacity, source.Transform?.ToImmutable(), 
                  source.TransformOrigin, source.SpreadMethod)
        {

        }

        /// <inheritdoc/>
        public IReadOnlyList<IGradientStop> GradientStops { get; }

        /// <inheritdoc/>
        public double Opacity { get; }

        /// <summary>
        /// Gets the transform of the brush.
        /// </summary>
        public ITransform? Transform { get; }

        /// <summary>
        /// Gets the transform origin of the brush
        /// </summary>
        public RelativePoint TransformOrigin { get; }

        /// <inheritdoc/>
        public GradientSpreadMethod SpreadMethod { get; }
    }
}
