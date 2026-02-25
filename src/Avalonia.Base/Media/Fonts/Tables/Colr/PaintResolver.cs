using System;
using System.Linq;
using Avalonia.Media.Immutable;

namespace Avalonia.Media.Fonts.Tables.Colr
{
    /// <summary>
    /// Resolves Paint definitions into ResolvedPaint by applying variation deltas and normalization.
    /// </summary>
    internal static class PaintResolver
    {
        /// <summary>
        /// Resolves a paint graph by evaluating variable and composite paint nodes into their fully realized, static forms
        /// using the provided color context.
        /// </summary>
        /// <remarks>This method recursively traverses the paint graph, resolving all variable, transform, and
        /// composite nodes into their static equivalents. The returned paint can be used for rendering or further
        /// processing without requiring additional context or variation data.</remarks>
        /// <param name="paint">The root paint node to resolve. This may be a variable, composite, or transform paint type.</param>
        /// <param name="context">The color context used to evaluate variable paint nodes and apply variation deltas.</param>
        /// <returns>A new paint instance representing the fully resolved, static form of the input paint. The returned paint will
        /// not contain any variable or unresolved composite nodes.</returns>
        /// <exception cref="NotSupportedException">Thrown if the type of the provided paint node is not recognized or supported for resolution.</exception>
        public static Paint ResolvePaint(Paint paint, in ColrContext context)
        {
            switch (paint)
            {
                // Format 1: ColrLayers
                case ColrLayers colrLayers:
                    {
                        var resolvedLayers = new Paint[colrLayers.Layers.Count];

                        for (int i = 0; i < colrLayers.Layers.Count; i++)
                        {
                            resolvedLayers[i] = ResolvePaint(colrLayers.Layers[i], context);
                        }

                        return new ColrLayers(resolvedLayers);
                    }

                // Format 2: Solid
                case Solid solid:
                    return new ResolvedSolid(solid.Color);

                // Format 3: VarSolid
                case SolidVar varSolid:
                    return new ResolvedSolid(
                        context.ApplyAlphaDelta(varSolid.Color, varSolid.VarIndexBase, 0)
                    );

                // Format 4: LinearGradient
                case LinearGradient linearGrad:
                    return ResolveLinearGradient(linearGrad, context);

                // Format 5: VarLinearGradient
                case LinearGradientVar varLinearGrad:
                    return ResolveLinearGradient(
                        varLinearGrad,
                        context,
                        varLinearGrad.VarIndexBase
                    );

                // Format 6: RadialGradient
                case RadialGradient radialGrad:
                    return ResolveRadialGradient(radialGrad, context);

                // Format 7: VarRadialGradient
                case RadialGradientVar varRadialGrad:
                    return ResolveRadialGradient(
                        varRadialGrad,
                        context,
                        varRadialGrad.VarIndexBase
                    );

                // Format 8: SweepGradient
                case SweepGradient sweepGrad:
                    return ResolveSweepGradient(sweepGrad, context);

                // Format 9: VarSweepGradient
                case SweepGradientVar varSweepGrad:
                    return ResolveSweepGradient(
                        varSweepGrad,
                        context,
                        varSweepGrad.VarIndexBase
                    );

                // Format 10: Glyph
                case Glyph glyph:
                    return new Glyph(glyph.GlyphId, ResolvePaint(glyph.Paint, context));

                // Format 11: ColrGlyph
                case ColrGlyph colrGlyph:
                    return ResolvePaintColrGlyph(colrGlyph, context);

                // Format 12: Transform
                case Transform transform:
                    return new ResolvedTransform(
                        transform.Matrix,
                        ResolvePaint(transform.Inner, context)
                    );

                // Format 13: VarTransform
                case TransformVar varTransform:
                    return new ResolvedTransform(
                        context.ApplyAffineDeltas(varTransform.Matrix, varTransform.VarIndexBase),
                        ResolvePaint(varTransform.Inner, context)
                    );

                // Format 14: Translate
                case Translate translate:
                    return new ResolvedTransform(
                        Matrix.CreateTranslation(translate.Dx, translate.Dy),
                        ResolvePaint(translate.Inner, context)
                    );

                // Format 15: VarTranslate
                case TranslateVar varTranslate:
                    {
                        var dx = varTranslate.Dx;
                        var dy = varTranslate.Dy;

                        if (context.ColrTable.TryGetVariationDeltaSet(varTranslate.VarIndexBase, out var deltaSet))
                        {
                            // Translate deltas are FWORD (design units)
                            if (deltaSet.Count > 0)
                                dx += deltaSet.GetFWordDelta(0);
                            if (deltaSet.Count > 1)
                                dy += deltaSet.GetFWordDelta(1);
                        }

                        return new ResolvedTransform(
                            Matrix.CreateTranslation(dx, dy),
                            ResolvePaint(varTranslate.Inner, context)
                        );
                    }

                // Format 16: Scale
                case Scale scale:
                    return new ResolvedTransform(
                        Matrix.CreateScale(scale.Sx, scale.Sy),
                        ResolvePaint(scale.Inner, context)
                    );

                // Format 17: VarScale
                case ScaleVar varScale:
                    {
                        var sx = varScale.Sx;
                        var sy = varScale.Sy;

                        if (context.ColrTable.TryGetVariationDeltaSet(varScale.VarIndexBase, out var deltaSet))
                        {
                            // Scale deltas are F2DOT14
                            if (deltaSet.Count > 0)
                                sx += deltaSet.GetF2Dot14Delta(0);
                            if (deltaSet.Count > 1)
                                sy += deltaSet.GetF2Dot14Delta(1);
                        }

                        return new ResolvedTransform(
                            Matrix.CreateScale(sx, sy),
                            ResolvePaint(varScale.Inner, context)
                        );
                    }

                // Format 18: ScaleAroundCenter
                case ScaleAroundCenter scaleCenter:
                    return new ResolvedTransform(
                        CreateScaleAroundCenter(scaleCenter.Sx, scaleCenter.Sy, scaleCenter.Center),
                        ResolvePaint(scaleCenter.Inner, context)
                    );

                // Format 19: VarScaleAroundCenter
                case ScaleAroundCenterVar varScaleCenter:
                    {
                        var sx = varScaleCenter.Sx;
                        var sy = varScaleCenter.Sy;
                        var centerX = varScaleCenter.Center.X;
                        var centerY = varScaleCenter.Center.Y;

                        if (context.ColrTable.TryGetVariationDeltaSet(varScaleCenter.VarIndexBase, out var deltaSet))
                        {
                            // Scale deltas are F2DOT14
                            if (deltaSet.Count > 0)
                                sx += deltaSet.GetF2Dot14Delta(0);
                            if (deltaSet.Count > 1)
                                sy += deltaSet.GetF2Dot14Delta(1);
                            // Center coordinate deltas are FWORD
                            if (deltaSet.Count > 2)
                                centerX += deltaSet.GetFWordDelta(2);
                            if (deltaSet.Count > 3)
                                centerY += deltaSet.GetFWordDelta(3);
                        }

                        return new ResolvedTransform(
                            CreateScaleAroundCenter(sx, sy, new Point(centerX, centerY)),
                            ResolvePaint(varScaleCenter.Inner, context)
                        );
                    }

                // Format 20: ScaleUniform
                case ScaleUniform scaleUniform:
                    return new ResolvedTransform(
                        Matrix.CreateScale(scaleUniform.Scale, scaleUniform.Scale),
                        ResolvePaint(scaleUniform.Inner, context)
                    );

                // Format 21: VarScaleUniform
                case ScaleUniformVar varScaleUniform:
                    {
                        var scale = varScaleUniform.Scale;

                        if (context.ColrTable.TryGetVariationDeltaSet(varScaleUniform.VarIndexBase, out var deltaSet))
                        {
                            // Scale delta is F2DOT14
                            if (deltaSet.Count > 0)
                                scale += deltaSet.GetF2Dot14Delta(0);
                        }

                        return new ResolvedTransform(
                            Matrix.CreateScale(scale, scale),
                            ResolvePaint(varScaleUniform.Inner, context)
                        );
                    }

                // Format 22: ScaleUniformAroundCenter
                case ScaleUniformAroundCenter scaleUniformCenter:
                    return new ResolvedTransform(
                        CreateScaleAroundCenter(
                            scaleUniformCenter.Scale,
                            scaleUniformCenter.Scale,
                            scaleUniformCenter.Center
                        ),
                        ResolvePaint(scaleUniformCenter.Inner, context)
                    );

                // Format 23: VarScaleUniformAroundCenter
                case ScaleUniformAroundCenterVar varScaleUniformCenter:
                    {
                        var scale = varScaleUniformCenter.Scale;
                        var centerX = varScaleUniformCenter.Center.X;
                        var centerY = varScaleUniformCenter.Center.Y;

                        if (context.ColrTable.TryGetVariationDeltaSet(varScaleUniformCenter.VarIndexBase, out var deltaSet))
                        {
                            // Scale delta is F2DOT14
                            if (deltaSet.Count > 0)
                                scale += deltaSet.GetF2Dot14Delta(0);
                            // Center coordinate deltas are FWORD
                            if (deltaSet.Count > 1)
                                centerX += deltaSet.GetFWordDelta(1);
                            if (deltaSet.Count > 2)
                                centerY += deltaSet.GetFWordDelta(2);
                        }

                        return new ResolvedTransform(
                            CreateScaleAroundCenter(scale, scale, new Point(centerX, centerY)),
                            ResolvePaint(varScaleUniformCenter.Inner, context)
                        );
                    }

                // Format 24: Rotate
                case Rotate rotate:
                    return new ResolvedTransform(
                        CreateRotation(rotate.Angle),
                        ResolvePaint(rotate.Inner, context)
                    );

                // Format 25: VarRotate
                case RotateVar varRotate:
                    {
                        var angle = varRotate.Angle;

                        if (context.ColrTable.TryGetVariationDeltaSet(varRotate.VarIndexBase, out var deltaSet))
                        {
                            // Angle delta is F2DOT14: 180° per 1.0, so multiply by π to convert to radians
                            if (deltaSet.Count > 0)
                                angle += deltaSet.GetF2Dot14Delta(0) * Math.PI;
                        }

                        return new ResolvedTransform(
                            CreateRotation(angle),
                            ResolvePaint(varRotate.Inner, context)
                        );
                    }

                // Format 26: RotateAroundCenter
                case RotateAroundCenter rotateCenter:
                    return new ResolvedTransform(
                        CreateRotation(rotateCenter.Angle, rotateCenter.Center),
                        ResolvePaint(rotateCenter.Inner, context)
                    );

                // Format 27: VarRotateAroundCenter
                case RotateAroundCenterVar varRotateCenter:
                    {
                        var angle = varRotateCenter.Angle;
                        var centerX = varRotateCenter.Center.X;
                        var centerY = varRotateCenter.Center.Y;

                        if (context.ColrTable.TryGetVariationDeltaSet(varRotateCenter.VarIndexBase, out var deltaSet))
                        {
                            // Angle delta is F2DOT14: 180° per 1.0, so multiply by π to convert to radians
                            if (deltaSet.Count > 0)
                                angle += deltaSet.GetF2Dot14Delta(0) * Math.PI;
                            // Center coordinate deltas are FWORD
                            if (deltaSet.Count > 1)
                                centerX += deltaSet.GetFWordDelta(1);
                            if (deltaSet.Count > 2)
                                centerY += deltaSet.GetFWordDelta(2);
                        }

                        return new ResolvedTransform(
                            CreateRotation(angle, new Point(centerX, centerY)),
                            ResolvePaint(varRotateCenter.Inner, context)
                        );
                    }

                // Format 28: Skew
                case Skew skew:
                    return new ResolvedTransform(
                        CreateSkew(skew.XAngle, skew.YAngle, new Point()),
                        ResolvePaint(skew.Inner, context)
                    );

                // Format 29: VarSkew
                case SkewVar varSkew:
                    {
                        var xAngle = varSkew.XAngle;
                        var yAngle = varSkew.YAngle;

                        if (context.ColrTable.TryGetVariationDeltaSet(varSkew.VarIndexBase, out var deltaSet))
                        {
                            // Angle deltas are F2DOT14: 180° per 1.0, so multiply by π to convert to radians
                            if (deltaSet.Count > 0)
                                xAngle += deltaSet.GetF2Dot14Delta(0) * Math.PI;
                            if (deltaSet.Count > 1)
                                yAngle += deltaSet.GetF2Dot14Delta(1) * Math.PI;
                        }

                        return new ResolvedTransform(
                            CreateSkew(xAngle, yAngle, new Point()),
                            ResolvePaint(varSkew.Inner, context)
                        );
                    }

                // Format 30: SkewAroundCenter
                case SkewAroundCenter skewCenter:
                    return new ResolvedTransform(
                        CreateSkew(skewCenter.XAngle, skewCenter.YAngle, skewCenter.Center),
                        ResolvePaint(skewCenter.Inner, context)
                    );

                // Format 31: VarSkewAroundCenter
                case SkewAroundCenterVar varSkewCenter:
                    {
                        var xAngle = varSkewCenter.XAngle;
                        var yAngle = varSkewCenter.YAngle;
                        var centerX = varSkewCenter.Center.X;
                        var centerY = varSkewCenter.Center.Y;

                        if (context.ColrTable.TryGetVariationDeltaSet(varSkewCenter.VarIndexBase, out var deltaSet))
                        {
                            // Angle deltas are F2DOT14: 180° per 1.0, so multiply by π to convert to radians
                            if (deltaSet.Count > 0)
                                xAngle += deltaSet.GetF2Dot14Delta(0) * Math.PI;
                            if (deltaSet.Count > 1)
                                yAngle += deltaSet.GetF2Dot14Delta(1) * Math.PI;
                            // Center coordinate deltas are FWORD
                            if (deltaSet.Count > 2)
                                centerX += deltaSet.GetFWordDelta(2);
                            if (deltaSet.Count > 3)
                                centerY += deltaSet.GetFWordDelta(3);
                        }

                        return new ResolvedTransform(
                            CreateSkew(xAngle, yAngle, new Point(centerX, centerY)),
                            ResolvePaint(varSkewCenter.Inner, context)
                        );
                    }

                // Format 32: Composite
                case Composite composite:
                    return new Composite(
                        ResolvePaint(composite.Backdrop, context),
                        ResolvePaint(composite.Source, context),
                        composite.Mode
                    );

                default:
                    throw new NotSupportedException($"Unknown paint type: {paint.GetType().Name}");
            }
        }

        internal static Paint ResolvePaintColrGlyph(ColrGlyph colrGlyph, ColrContext context)
        {
            var glyphId = colrGlyph.GlyphId;
            // Resolve inner paint
            var resolvedInner = ResolvePaint(colrGlyph.Inner, context);

            // Wrap in a clip box if present
            if (context.ColrTable.TryGetClipBox(glyphId, out var clipBox))
            {
                return new ResolvedClipBox(clipBox, resolvedInner);
            }

            return resolvedInner;
        }

        private static ResolvedPaint ResolveLinearGradient(LinearGradient grad, ColrContext context)
        {
            var stops = context.NormalizeColorStops(grad.Stops);

            return NormalizeLinearGradient(grad.P0, grad.P1, grad.P2, stops, grad.Extend);
        }

        private static ResolvedPaint ResolveLinearGradient(LinearGradientVar grad, ColrContext context, uint varIndexBase)
        {
            var p0 = grad.P0;
            var p1 = grad.P1;
            var p2 = grad.P2;

            if (context.ColrTable.TryGetVariationDeltaSet(varIndexBase, out var deltaSet))
            {
                // Gradient coordinate deltas are FWORD (design units)
                if (deltaSet.Count > 0)
                    p0 = new Point(p0.X + deltaSet.GetFWordDelta(0), p0.Y);
                if (deltaSet.Count > 1)
                    p0 = new Point(p0.X, p0.Y + deltaSet.GetFWordDelta(1));
                if (deltaSet.Count > 2)
                    p1 = new Point(p1.X + deltaSet.GetFWordDelta(2), p1.Y);
                if (deltaSet.Count > 3)
                    p1 = new Point(p1.X, p1.Y + deltaSet.GetFWordDelta(3));
                if (deltaSet.Count > 4)
                    p2 = new Point(p2.X + deltaSet.GetFWordDelta(4), p2.Y);
                if (deltaSet.Count > 5)
                    p2 = new Point(p2.X, p2.Y + deltaSet.GetFWordDelta(5));
            }

            var stops = context.ResolveColorStops(grad.Stops, varIndexBase);
            return NormalizeLinearGradient(p0, p1, p2, stops, grad.Extend);
        }

        private static ResolvedPaint ResolveRadialGradient(RadialGradient grad, ColrContext context)
        {
            var stops = context.NormalizeColorStops(grad.Stops);
            return new ResolvedRadialGradient(grad.C0, grad.R0, grad.C1, grad.R1, stops, grad.Extend);
        }

        private static ResolvedPaint ResolveRadialGradient(RadialGradientVar grad, ColrContext context, uint varIndexBase)
        {
            var c0 = grad.C0;
            var r0 = grad.R0;
            var c1 = grad.C1;
            var r1 = grad.R1;

            if (context.ColrTable.TryGetVariationDeltaSet(varIndexBase, out var deltaSet))
            {
                // Center coordinate deltas and radii deltas are FWORD (design units)
                if (deltaSet.Count > 0)
                    c0 = new Point(c0.X + deltaSet.GetFWordDelta(0), c0.Y);
                if (deltaSet.Count > 1)
                    c0 = new Point(c0.X, c0.Y + deltaSet.GetFWordDelta(1));
                if (deltaSet.Count > 2)
                    r0 += deltaSet.GetFWordDelta(2);
                if (deltaSet.Count > 3)
                    c1 = new Point(c1.X + deltaSet.GetFWordDelta(3), c1.Y);
                if (deltaSet.Count > 4)
                    c1 = new Point(c1.X, c1.Y + deltaSet.GetFWordDelta(4));
                if (deltaSet.Count > 5)
                    r1 += deltaSet.GetFWordDelta(5);
            }

            var stops = context.ResolveColorStops(grad.Stops, varIndexBase);
            return new ResolvedRadialGradient(c0, r0, c1, r1, stops, grad.Extend);
        }

        private static ResolvedPaint ResolveSweepGradient(SweepGradient grad, ColrContext context)
        {
            var stops = context.NormalizeColorStops(grad.Stops);
            return NormalizeConicGradient(grad.Center, grad.StartAngle, grad.EndAngle, stops, grad.Extend);
        }

        private static ResolvedPaint ResolveSweepGradient(SweepGradientVar grad, ColrContext context, uint varIndexBase)
        {
            var center = grad.Center;
            var startAngle = grad.StartAngle;
            var endAngle = grad.EndAngle;

            if (context.ColrTable.TryGetVariationDeltaSet(varIndexBase, out var deltaSet))
            {

                // Center coordinate deltas are FWORD (design units)
                if (deltaSet.Count > 0)
                    center = new Point(center.X + deltaSet.GetFWordDelta(0), center.Y);
                if (deltaSet.Count > 1)
                    center = new Point(center.X, center.Y + deltaSet.GetFWordDelta(1));
                // Angle deltas are F2DOT14: 180° per 1.0, so multiply by π to convert to radians
                if (deltaSet.Count > 2)
                    startAngle += deltaSet.GetF2Dot14Delta(2) * Math.PI;
                if (deltaSet.Count > 3)
                    endAngle += deltaSet.GetF2Dot14Delta(3) * Math.PI;
            }

            var stops = context.ResolveColorStops(grad.Stops, varIndexBase);
            return NormalizeConicGradient(center, startAngle, endAngle, stops, grad.Extend);
        }

        private static ResolvedPaint NormalizeLinearGradient(
            Point p0, Point p1, Point p2,
            GradientStop[] stops,
            GradientSpreadMethod extend)
        {
            // If no stops or single stop, return solid color
            if (stops.Length == 0)
                return new ResolvedSolid(Colors.Transparent);

            if (stops.Length == 1)
                return new ResolvedSolid(stops[0].Color);

            // If p0p1 or p0p2 are degenerate, use first color
            var p0ToP1 = p1 - p0;
            var p0ToP2 = p2 - p0;

            if (IsDegenerate(p0ToP1) || IsDegenerate(p0ToP2) ||
                Math.Abs(CrossProduct(p0ToP1, p0ToP2)) < 1e-6)
            {
                return new ResolvedSolid(stops[0].Color);
            }

            // Compute P3 as orthogonal projection of p0->p1 onto perpendicular to p0->p2
            var perpToP2 = new Vector(p0ToP2.Y, -p0ToP2.X);
            var p3 = p0 + ProjectOnto(p0ToP1, perpToP2);

            return new ResolvedLinearGradient(p0, p3, stops, extend);
        }

        private static ResolvedPaint NormalizeConicGradient(
            Point center,
            double startAngle,
            double endAngle,
            GradientStop[] stops,
            GradientSpreadMethod extend)
        {
            if (stops.Length == 0)
                return new ResolvedSolid(Colors.Transparent);

            if (stops.Length == 1)
                return new ResolvedSolid(stops[0].Color);

            // OpenType 1.9.1 adds a shift to ease 0-360 degree specification
            var startAngleDeg = startAngle * 180.0 + 180.0;
            var endAngleDeg = endAngle * 180.0 + 180.0;

            // Convert from counter-clockwise to clockwise
            startAngleDeg = 360.0 - startAngleDeg;
            endAngleDeg = 360.0 - endAngleDeg;

            var finalStops = stops;

            // Swap if needed to ensure start < end
            if (startAngleDeg > endAngleDeg)
            {
                (startAngleDeg, endAngleDeg) = (endAngleDeg, startAngleDeg);

                // Reverse stops - only allocate if we need to reverse
                finalStops = ReverseStops(stops);
            }

            // If start == end and not Pad mode, nothing should be drawn
            if (Math.Abs(startAngleDeg - endAngleDeg) < 1e-6 && extend != GradientSpreadMethod.Pad)
            {
                return new ResolvedSolid(Colors.Transparent);
            }

            return new ResolvedConicGradient(center, startAngleDeg, endAngleDeg, finalStops, extend);
        }

        private static GradientStop[] ReverseStops(GradientStop[] stops)
        {
            var length = stops.Length;
            var reversed = new GradientStop[length];

            for (int i = 0; i < length; i++)
            {
                var originalStop = stops[length - 1 - i];
                reversed[i] = new GradientStop(1.0 - originalStop.Offset, originalStop.Color);
            }

            return reversed;
        }

        private static Matrix CreateScaleAroundCenter(double sx, double sy, Point center)
        {
            return Matrix.CreateTranslation(-center.X, -center.Y) *
                   Matrix.CreateScale(sx, sy) *
                   Matrix.CreateTranslation(center.X, center.Y);
        }

        private static Matrix CreateRotation(double angleRadians)
        {
            return Matrix.CreateRotation(angleRadians);
        }

        private static Matrix CreateRotation(double angleRadians, Point center)
        {
            return Matrix.CreateTranslation(-center.X, -center.Y) *
                   Matrix.CreateRotation(angleRadians) *
                   Matrix.CreateTranslation(center.X, center.Y);
        }

        private static Matrix CreateSkew(double xAngleRadians, double yAngleRadians, Point center)
        {
            var skewMatrix = new Matrix(
                1.0, Math.Tan(yAngleRadians),
                Math.Tan(xAngleRadians), 1.0,
                0.0, 0.0
            );

            if (center == default)
                return skewMatrix;

            return Matrix.CreateTranslation(-center.X, -center.Y) *
                   skewMatrix *
                   Matrix.CreateTranslation(center.X, center.Y);
        }

        private static bool IsDegenerate(Vector v)
        {
            return Math.Abs(v.X) < 1e-6 && Math.Abs(v.Y) < 1e-6;
        }

        private static double CrossProduct(Vector a, Vector b)
        {
            return a.X * b.Y - a.Y * b.X;
        }

        private static double DotProduct(Vector a, Vector b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        private static Vector ProjectOnto(Vector vector, Vector onto)
        {
            var length = Math.Sqrt(onto.X * onto.X + onto.Y * onto.Y);
            if (length < 1e-6)
                return new Vector(0, 0);

            var normalized = onto / length;
            var scale = DotProduct(vector, onto) / length;
            return normalized * scale;
        }
    }
}
