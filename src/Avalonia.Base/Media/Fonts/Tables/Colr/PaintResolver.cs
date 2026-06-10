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
                        // Translate deltas are FWORD (design units): dx = base+0, dy = base+1.
                        var dx = varTranslate.Dx + context.GetFWordDelta(varTranslate.VarIndexBase, 0);
                        var dy = varTranslate.Dy + context.GetFWordDelta(varTranslate.VarIndexBase, 1);

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
                        // Scale deltas are F2DOT14: sx = base+0, sy = base+1.
                        var sx = varScale.Sx + context.GetF2Dot14Delta(varScale.VarIndexBase, 0);
                        var sy = varScale.Sy + context.GetF2Dot14Delta(varScale.VarIndexBase, 1);

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
                        var b = varScaleCenter.VarIndexBase;
                        // Scale deltas F2DOT14 (base+0,+1); centre deltas FWORD (base+2,+3).
                        var sx = varScaleCenter.Sx + context.GetF2Dot14Delta(b, 0);
                        var sy = varScaleCenter.Sy + context.GetF2Dot14Delta(b, 1);
                        var centerX = varScaleCenter.Center.X + context.GetFWordDelta(b, 2);
                        var centerY = varScaleCenter.Center.Y + context.GetFWordDelta(b, 3);

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
                        // Scale delta is F2DOT14: scale = base+0.
                        var scale = varScaleUniform.Scale + context.GetF2Dot14Delta(varScaleUniform.VarIndexBase, 0);

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
                        var b = varScaleUniformCenter.VarIndexBase;
                        // Scale delta F2DOT14 (base+0); centre deltas FWORD (base+1,+2).
                        var scale = varScaleUniformCenter.Scale + context.GetF2Dot14Delta(b, 0);
                        var centerX = varScaleUniformCenter.Center.X + context.GetFWordDelta(b, 1);
                        var centerY = varScaleUniformCenter.Center.Y + context.GetFWordDelta(b, 2);

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
                        // Angle delta is F2DOT14: 180° per 1.0, so multiply by π to convert to radians.
                        var angle = varRotate.Angle + context.GetF2Dot14Delta(varRotate.VarIndexBase, 0) * Math.PI;

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
                        var b = varRotateCenter.VarIndexBase;
                        // Angle delta F2DOT14 → radians (base+0); centre deltas FWORD (base+1,+2).
                        var angle = varRotateCenter.Angle + context.GetF2Dot14Delta(b, 0) * Math.PI;
                        var centerX = varRotateCenter.Center.X + context.GetFWordDelta(b, 1);
                        var centerY = varRotateCenter.Center.Y + context.GetFWordDelta(b, 2);

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
                        // Angle deltas are F2DOT14: 180° per 1.0, so multiply by π to convert to radians.
                        var xAngle = varSkew.XAngle + context.GetF2Dot14Delta(varSkew.VarIndexBase, 0) * Math.PI;
                        var yAngle = varSkew.YAngle + context.GetF2Dot14Delta(varSkew.VarIndexBase, 1) * Math.PI;

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
                        var b = varSkewCenter.VarIndexBase;
                        // Angle deltas F2DOT14 → radians (base+0,+1); centre deltas FWORD (base+2,+3).
                        var xAngle = varSkewCenter.XAngle + context.GetF2Dot14Delta(b, 0) * Math.PI;
                        var yAngle = varSkewCenter.YAngle + context.GetF2Dot14Delta(b, 1) * Math.PI;
                        var centerX = varSkewCenter.Center.X + context.GetFWordDelta(b, 2);
                        var centerY = varSkewCenter.Center.Y + context.GetFWordDelta(b, 3);

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
            if (context.ColrTable.TryGetClipBox(glyphId, context.ActiveCoords, out var clipBox))
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
            // Gradient coordinate deltas are FWORD (design units): p0.x/.y, p1.x/.y, p2.x/.y = base+0..5.
            var p0 = new Point(grad.P0.X + context.GetFWordDelta(varIndexBase, 0), grad.P0.Y + context.GetFWordDelta(varIndexBase, 1));
            var p1 = new Point(grad.P1.X + context.GetFWordDelta(varIndexBase, 2), grad.P1.Y + context.GetFWordDelta(varIndexBase, 3));
            var p2 = new Point(grad.P2.X + context.GetFWordDelta(varIndexBase, 4), grad.P2.Y + context.GetFWordDelta(varIndexBase, 5));

            var stops = context.NormalizeColorStops(grad.Stops);
            return NormalizeLinearGradient(p0, p1, p2, stops, grad.Extend);
        }

        private static ResolvedPaint ResolveRadialGradient(RadialGradient grad, ColrContext context)
        {
            var stops = context.NormalizeColorStops(grad.Stops);
            return new ResolvedRadialGradient(grad.C0, grad.R0, grad.C1, grad.R1, stops, grad.Extend);
        }

        private static ResolvedPaint ResolveRadialGradient(RadialGradientVar grad, ColrContext context, uint varIndexBase)
        {
            // Centre coordinate and radii deltas are FWORD (design units): c0.x/.y, r0, c1.x/.y, r1 = base+0..5.
            var c0 = new Point(grad.C0.X + context.GetFWordDelta(varIndexBase, 0), grad.C0.Y + context.GetFWordDelta(varIndexBase, 1));
            var r0 = grad.R0 + context.GetFWordDelta(varIndexBase, 2);
            var c1 = new Point(grad.C1.X + context.GetFWordDelta(varIndexBase, 3), grad.C1.Y + context.GetFWordDelta(varIndexBase, 4));
            var r1 = grad.R1 + context.GetFWordDelta(varIndexBase, 5);

            var stops = context.NormalizeColorStops(grad.Stops);
            return new ResolvedRadialGradient(c0, r0, c1, r1, stops, grad.Extend);
        }

        private static ResolvedPaint ResolveSweepGradient(SweepGradient grad, ColrContext context)
        {
            var stops = context.NormalizeColorStops(grad.Stops);
            return NormalizeConicGradient(grad.Center, grad.StartAngle, grad.EndAngle, stops, grad.Extend);
        }

        private static ResolvedPaint ResolveSweepGradient(SweepGradientVar grad, ColrContext context, uint varIndexBase)
        {
            // Centre deltas FWORD (base+0,+1); angle deltas F2DOT14 → radians (base+2,+3).
            var center = new Point(grad.Center.X + context.GetFWordDelta(varIndexBase, 0), grad.Center.Y + context.GetFWordDelta(varIndexBase, 1));
            var startAngle = grad.StartAngle + context.GetF2Dot14Delta(varIndexBase, 2) * Math.PI;
            var endAngle = grad.EndAngle + context.GetF2Dot14Delta(varIndexBase, 3) * Math.PI;

            var stops = context.NormalizeColorStops(grad.Stops);
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
