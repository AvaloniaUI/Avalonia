using System;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// A node in the scene graph which represents a glyph run draw.
    /// </summary>
    internal class GlyphRunNode : BrushDrawOperation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphRunNode"/> class.
        /// </summary>
        /// <param name="transform">The transform.</param>
        /// <param name="foreground">The foreground brush.</param>
        /// <param name="glyphRun">The glyph run to draw.</param>
        public GlyphRunNode(
            Matrix transform,
            IImmutableBrush foreground,
            IRef<IGlyphRunImpl> glyphRun)
            : base(glyphRun.Item.Bounds, transform, foreground)
        {
            GlyphRun = glyphRun.Clone();
        }
        
        
        /// <summary>
        /// Gets the glyph run to draw.
        /// </summary>
        public IRef<IGlyphRunImpl> GlyphRun { get; }

        /// <inheritdoc/>
        public override void Render(IDrawingContextImpl context) => context.DrawGlyphRun(Brush, GlyphRun);

        /// <summary>
        /// Determines if this draw operation equals another.
        /// </summary>
        /// <param name="transform">The transform of the other draw operation.</param>
        /// <param name="foreground">The foreground of the other draw operation.</param>
        /// <param name="glyphRun">The glyph run of the other draw operation.</param>
        /// <returns>True if the draw operations are the same, otherwise false.</returns>
        /// <remarks>
        /// The properties of the other draw operation are passed in as arguments to prevent
        /// allocation of a not-yet-constructed draw operation object.
        /// </remarks>
        internal bool Equals(Matrix transform, IBrush foreground, IRef<IGlyphRunImpl> glyphRun)
        {
            return transform == Transform &&
                   Equals(foreground, Brush) &&
                   Equals(glyphRun.Item, GlyphRun.Item);
        }

        /// <inheritdoc/>
        public override bool HitTestTransformed(Point p)
        {
            return GlyphRun.Item.Bounds.ContainsExclusive(p);
        }

        public override void Dispose()
        {
            GlyphRun?.Dispose();
            base.Dispose();
        }
    }
}
