namespace Avalonia.Media
{
    /// <summary>
    /// Abstract class that describes a 2-D drawing.
    /// </summary>
    public abstract class Drawing : AvaloniaObject
    {
        internal Drawing()
        {
            
        }
        
        /// <summary>
        /// Draws this drawing to the given <see cref="DrawingContext"/>.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        public void Draw(DrawingContext context) => DrawCore(context);

        internal abstract void DrawCore(DrawingContext context);

        /// <summary>
        /// Gets the drawing's bounding rectangle.
        /// </summary>
        public abstract Rect GetBounds();

        /// <summary>
        /// Gets the drawing's outer bounding rectangle including visual effects
        /// (e.g., drop shadows) applied within the drawing.
        /// </summary>
        /// <remarks>
        /// Default implementation returns <see cref="GetBounds"/>. Drawings that can
        /// produce pixels outside their geometric/content bounds (such as <see cref="DrawingGroup"/>
        /// with an <see cref="IEffect"/>) should override this to include such output.
        /// </remarks>
        public virtual Rect GetOuterBounds() => GetBounds();

        /// <summary>
        /// Gets the content bounds used when an effect is applied, i.e. the pre-inflation
        /// bounds that effects operate on. Default implementation returns <see cref="GetBounds"/>.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="GetOuterBounds"/>, this does NOT include effect padding inflation.
        /// For a <see cref="DrawingGroup"/> with an effect, this should return the effect's
        /// content bounds (e.g., the rect passed to <see cref="DrawingContext.PushEffect"/>),
        /// transformed appropriately.
        /// </remarks>
        public virtual Rect GetEffectContentBounds() => GetBounds();
    }
}
