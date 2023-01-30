using Avalonia.Media.Immutable;
using Avalonia.VisualTree;

namespace Avalonia.Media
{
    /// <summary>
    /// Paints an area with an <see cref="Visual"/>.
    /// </summary>
    public class VisualBrush : TileBrush, IVisualBrush, IMutableBrush
    {
        /// <summary>
        /// Defines the <see cref="Visual"/> property.
        /// </summary>
        public static readonly StyledProperty<Visual> VisualProperty =
            AvaloniaProperty.Register<VisualBrush, Visual>(nameof(Visual));

        static VisualBrush()
        {
            AffectsRender<VisualBrush>(VisualProperty);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualBrush"/> class.
        /// </summary>
        public VisualBrush()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualBrush"/> class.
        /// </summary>
        /// <param name="visual">The visual to draw.</param>
        public VisualBrush(Visual visual)
        {
            Visual = visual;
        }

        /// <summary>
        /// Gets or sets the visual to draw.
        /// </summary>
        public Visual Visual
        {
            get { return GetValue(VisualProperty); }
            set { SetValue(VisualProperty, value); }
        }

        /// <inheritdoc/>
        IImmutableBrush IMutableBrush.ToImmutable()
        {
            return new ImmutableVisualBrush(this);
        }
    }
}
