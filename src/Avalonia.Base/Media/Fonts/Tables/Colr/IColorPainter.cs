using Avalonia.Media.Immutable;

namespace Avalonia.Media.Fonts.Tables.Colr
{
    internal interface IColorPainter
    {
        /// <summary>
        /// Pushes the specified transformation matrix onto the current transformation stack.
        /// </summary>
        /// <remarks>Use this method to temporarily modify the coordinate system for drawing operations.
        /// To restore the previous transformation, call the corresponding pop method if available. Transformations are
        /// applied in a last-in, first-out manner.</remarks>
        /// <param name="transform">The transformation matrix to apply to subsequent drawing operations. The matrix defines how coordinates are
        /// transformed, such as by translation, rotation, or scaling.</param>
        void PushTransform(Matrix transform);

        /// <summary>
        /// Removes the most recently applied transformation from the transformation stack.
        /// </summary>
        /// <remarks>Call this method to revert to the previous transformation state. Typically used in
        /// graphics or rendering contexts to restore the coordinate system after applying temporary transformations.
        /// Calling this method when the transformation stack is empty may result in an error or undefined behavior,
        /// depending on the implementation.</remarks>
        void PopTransform();

        /// <summary>
        /// Pushes a new drawing layer onto the stack using the specified composite mode.
        /// </summary>
        /// <remarks>Use this method to isolate drawing operations on a separate layer, which can then be
        /// composited with the underlying content according to the specified mode. Layers should typically be paired
        /// with a corresponding pop operation to restore the previous drawing state.</remarks>
        /// <param name="mode">The blending mode to use when compositing the new layer with existing content.</param>
        void PushLayer(CompositeMode mode);

        /// <summary>
        /// Removes the topmost layer from the current layer stack.
        /// </summary>
        /// <remarks>Call this method to revert to the previous layer after pushing a new one. If there
        /// are no layers to remove, the behavior may depend on the implementation; consult the specific documentation
        /// for details.</remarks>
        void PopLayer();

        /// <summary>
        /// Establishes a new clipping region that restricts drawing to the specified rectangle.
        /// </summary>
        /// <remarks>Subsequent drawing operations are limited to the area defined by the clipping region
        /// until the region is removed. Clipping regions can typically be nested; ensure that each call to this method
        /// is balanced with a corresponding call to remove the clipping region, if required by the
        /// implementation.</remarks>
        /// <param name="clipBox">The rectangle that defines the boundaries of the clipping region. Only drawing operations within this area
        /// will be visible.</param>
        void PushClip(Rect clipBox);

        /// <summary>
        /// Removes the most recently added clip from the stack.
        /// </summary>
        /// <remarks>Call this method to revert the last PushClip operation and restore the previous
        /// clipping region. If there are no clips on the stack, calling this method has no effect.</remarks>
        void PopClip();

        /// <summary>
        /// Fills the current path with a solid color.
        /// </summary>
        /// <param name="color"></param>
        void FillSolid(Color color);

        /// <summary>
        /// Fills the current path with a linear gradient defined by the specified points and gradient stops.
        /// </summary>
        /// <remarks>The gradient is interpolated between the specified points using the provided gradient
        /// stops. The spread method determines how the gradient is rendered outside the range defined by the start and
        /// end points.</remarks>
        /// <param name="p0">The starting point of the linear gradient in the coordinate space.</param>
        /// <param name="p1">The ending point of the linear gradient in the coordinate space.</param>
        /// <param name="stops">An array of gradient stops that define the colors and their positions along the gradient. Cannot be null or
        /// empty.</param>
        /// <param name="extend">Specifies how the gradient is extended beyond the start and end points, using the defined spread method.</param>
        void FillLinearGradient(Point p0, Point p1, GradientStop[] stops, GradientSpreadMethod extend);

        /// <summary>
        /// Fills the current path with a radial gradient defined by two circles and a set of gradient stops.
        /// </summary>
        /// <remarks>The gradient transitions from the color at the starting circle to the color at the
        /// ending circle, interpolating colors as defined by the gradient stops. The spread method determines how the
        /// gradient is rendered outside the circles' bounds.</remarks>
        /// <param name="c0">The center point of the starting circle for the gradient.</param>
        /// <param name="r0">The radius of the starting circle. Must be non-negative.</param>
        /// <param name="c1">The center point of the ending circle for the gradient.</param>
        /// <param name="r1">The radius of the ending circle. Must be non-negative.</param>
        /// <param name="stops">An array of gradient stops that define the colors and their positions within the gradient. Cannot be null or
        /// empty.</param>
        /// <param name="extend">Specifies how the gradient is extended beyond its normal range.</param>
        void FillRadialGradient(Point c0, double r0, Point c1, double r1, GradientStop[] stops, GradientSpreadMethod extend);

        /// <summary>
        /// Fills the current path with a conic gradient defined by the given center point, angle range, color stops,
        /// and spread method.
        /// </summary>
        /// <remarks>The conic gradient is drawn by interpolating colors between the specified stops along
        /// the angular range from <paramref name="startAngle"/> to <paramref name="endAngle"/>. The behavior outside
        /// this range is determined by the <paramref name="extend"/> parameter.</remarks>
        /// <param name="center">The center point of the conic gradient, specified in the coordinate space of the drawing surface.</param>
        /// <param name="startAngle">The starting angle, in degrees, at which the gradient begins. Measured clockwise from the positive X-axis.</param>
        /// <param name="endAngle">The ending angle, in degrees, at which the gradient ends. Measured clockwise from the positive X-axis. Must
        /// be greater than or equal to <paramref name="startAngle"/>.</param>
        /// <param name="stops">An array of <see cref="GradientStop"/> objects that define the colors and their positions within the
        /// gradient. Must contain at least two elements.</param>
        /// <param name="extend">A value that specifies how the gradient is extended beyond its normal range, as defined by the <see
        /// cref="GradientSpreadMethod"/> enumeration.</param>
        void FillConicGradient(Point center, double startAngle, double endAngle, GradientStop[] stops, GradientSpreadMethod extend);

        /// <summary>
        /// Pushes a glyph outline onto the painter's current state as the active path for subsequent fill operations.
        /// </summary>
        /// <param name="glyphId"></param>
        void Glyph(ushort glyphId);
    }
}
