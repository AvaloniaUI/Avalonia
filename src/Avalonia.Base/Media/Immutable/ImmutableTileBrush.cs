using Avalonia.Media.Imaging;

namespace Avalonia.Media.Immutable
{
    /// <summary>
    /// A brush which displays a repeating image.
    /// </summary>
    public abstract class ImmutableTileBrush : ITileBrush, IImmutableBrush
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageBrush"/> class.
        /// </summary>
        /// <param name="alignmentX">The horizontal alignment of a tile in the destination.</param>
        /// <param name="alignmentY">The vertical alignment of a tile in the destination.</param>
        /// <param name="destinationRect">The rectangle on the destination in which to paint a tile.</param>
        /// <param name="opacity">The opacity of the brush.</param>
        /// <param name="transform">The transform of the brush.</param>
        /// <param name="transformOrigin">The transform origin of the brush</param>
        /// <param name="sourceRect">The rectangle of the source image that will be displayed.</param>
        /// <param name="stretch">
        /// How the source rectangle will be stretched to fill the destination rect.
        /// </param>
        /// <param name="tileMode">The tile mode.</param>
        private protected ImmutableTileBrush(
            AlignmentX alignmentX,
            AlignmentY alignmentY,
            RelativeRect destinationRect,
            double opacity,
            ImmutableTransform? transform,
            RelativePoint transformOrigin,
            RelativeRect sourceRect,
            Stretch stretch,
            TileMode tileMode)
        {
            AlignmentX = alignmentX;
            AlignmentY = alignmentY;
            DestinationRect = destinationRect;
            Opacity = opacity;
            Transform = transform;
            TransformOrigin = transformOrigin;
            SourceRect = sourceRect;
            Stretch = stretch;
            TileMode = tileMode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageBrush"/> class.
        /// </summary>
        /// <param name="source">The brush from which this brush's properties should be copied.</param>
        protected ImmutableTileBrush(ITileBrush source)
            : this(
                  source.AlignmentX,
                  source.AlignmentY,
                  source.DestinationRect,
                  source.Opacity,
                  source.Transform?.ToImmutable(),
                  source.TransformOrigin,
                  source.SourceRect,
                  source.Stretch,
                  source.TileMode)
        {
        }

        /// <inheritdoc/>
        public AlignmentX AlignmentX { get; }

        /// <inheritdoc/>
        public AlignmentY AlignmentY { get; }

        /// <inheritdoc/>
        public RelativeRect DestinationRect { get; }

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
        public RelativeRect SourceRect { get; }

        /// <inheritdoc/>
        public Stretch Stretch { get; }

        /// <inheritdoc/>
        public TileMode TileMode { get; }
    }
}
