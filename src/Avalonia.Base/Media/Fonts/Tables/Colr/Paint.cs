using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Media.Immutable;

namespace Avalonia.Media.Fonts.Tables.Colr
{
    internal record GradientStop(double Offset, Color Color);
    internal record GradientStopVar(double Offset, Color Color, uint VarIndexBase) : GradientStop(Offset, Color);

    internal abstract record Paint { }

    // format 1
    internal record ColrLayers(IReadOnlyList<Paint> Layers) : Paint
    {
        public static bool TryParse(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context, in PaintDecycler decycler, out Paint? paint)
        {
            paint = null;

            if (span.Length < 6)
                return false;

            var numLayers = span[1];
            var firstLayerIndex = (int)BinaryPrimitives.ReadUInt32BigEndian(span.Slice(2));

            // LayerList structure:
            // - uint32: numLayers (count of all paint offsets in the list)
            // - Offset32[numLayers]: array of paint offsets
            //
            // firstLayerIndex is the absolute index (0-based) into this array
            // where this glyph's layers start.

            var layerListOffset = context.ColrTable.LayerV1ListOffset;

            // Skip the 4-byte count field at the start of LayerList
            var paintOffsetsStart = layerListOffset + 4;

            // Calculate the byte offset for the first paint offset of this glyph's layers
            // Each offset is 4 bytes (Offset32)
            var firstPaintOffsetPos = paintOffsetsStart + (firstLayerIndex * 4);

            // Ensure we have enough data for all the paint offsets we need to read
            var requiredBytes = firstPaintOffsetPos + (numLayers * 4);
            if (requiredBytes > context.ColrData.Length)
            {
                return false;
            }

            var paints = new List<Paint>((int)numLayers);

            for (int i = 0; i < numLayers; i++)
            {
                // Read the paint offset for this layer
                var paintOffsetPos = firstPaintOffsetPos + (i * 4);
                var layerPaintOffset = BinaryPrimitives.ReadUInt32BigEndian(
                    context.ColrData.Span.Slice((int)paintOffsetPos, 4));

                // The paint offset is relative to the start of the LayerList table
                var absolutePaintOffset = layerListOffset + layerPaintOffset;

                if (absolutePaintOffset >= context.ColrData.Length)
                {
                    continue;
                }

                if (PaintParser.TryParse(context.ColrData.Span, absolutePaintOffset, in context, in decycler, out var childPaint) && childPaint != null)
                {
                    paints.Add(childPaint);
                }
            }

            paint = new ColrLayers(paints);
            return true;
        }
    }

    // format 2, 3
    internal record Solid(Color Color) : Paint
    {
        public static bool TryParse(ReadOnlySpan<byte> span, in ColrContext context, out Paint? paint)
        {
            return TryParseInternal(span, in context, isVariable: false, out paint);
        }

        internal static bool TryParseInternal(ReadOnlySpan<byte> span, in ColrContext context, bool isVariable, out Paint? paint)
        {
            paint = null;

            var minSize = isVariable ? 9 : 5;

            if (span.Length < minSize)
            {
                return false;
            }

            var paletteIndex = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(1));
            var alphaFixed = BinaryPrimitives.ReadInt16BigEndian(span.Slice(3));
            var alpha = PaintParsingHelpers.F2Dot14ToDouble(alphaFixed);

            if (!context.CpalTable.TryGetColor(context.PaletteIndex, paletteIndex, out var color))
            {
                color = Colors.Black;
            }

            color = Color.FromArgb((byte)(color.A * alpha), color.R, color.G, color.B);

            if (isVariable)
            {
                var varIndexBase = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(5));

                paint = new SolidVar(color, varIndexBase);
            }
            else
            {
                paint = new Solid(color);
            }

            return true;
        }
    }

    internal record SolidVar(Color Color, uint VarIndexBase) : Paint
    {
        public static bool TryParse(ReadOnlySpan<byte> span, in ColrContext context, out Paint? paint)
        {
            return Solid.TryParseInternal(span, in context, isVariable: true, out paint);
        }
    }

    // format 4, 5
    internal record LinearGradient(
        Point P0, Point P1, Point P2,
        GradientStop[] Stops,
        GradientSpreadMethod Extend) : Paint
    {
        public static bool TryParse(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context, out Paint? paint)
        {
            return TryParseInternal(span, paintOffset, in context, isVariable: false, out paint);
        }

        internal static bool TryParseInternal(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context, bool isVariable, out Paint? paint)
        {
            paint = null;

            var minSize = isVariable ? 20 : 16;

            if (span.Length < minSize)
            {
                return false;
            }

            var colorLineOffset = PaintParsingHelpers.ReadOffset24(span.Slice(1));
            var x0 = BinaryPrimitives.ReadInt16BigEndian(span.Slice(4));
            var y0 = BinaryPrimitives.ReadInt16BigEndian(span.Slice(6));
            var x1 = BinaryPrimitives.ReadInt16BigEndian(span.Slice(8));
            var y1 = BinaryPrimitives.ReadInt16BigEndian(span.Slice(10));
            var x2 = BinaryPrimitives.ReadInt16BigEndian(span.Slice(12));
            var y2 = BinaryPrimitives.ReadInt16BigEndian(span.Slice(14));

            if (!PaintParsingHelpers.TryParseColorLine(
                context.ColrData.Span,
                paintOffset + colorLineOffset,
                in context,
                isVarColorLine: isVariable,
                out var immutableStops,
                out var extend))
            {
                return false;
            }

            if (isVariable)
            {
                var varIndexBase = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(16));
                var stops = new GradientStopVar[immutableStops.Length];

                for (int i = 0; i < immutableStops.Length; i++)
                {
                    stops[i] = new GradientStopVar(immutableStops[i].Offset, immutableStops[i].Color, 0);
                }

                paint = new LinearGradientVar(new Point(x0, y0), new Point(x1, y1), new Point(x2, y2), stops, extend, varIndexBase);
            }
            else
            {
                var stops = new GradientStop[immutableStops.Length];

                for (int i = 0; i < immutableStops.Length; i++)
                {
                    stops[i] = new GradientStop(immutableStops[i].Offset, immutableStops[i].Color);
                }

                paint = new LinearGradient(new Point(x0, y0), new Point(x1, y1), new Point(x2, y2), stops, extend);
            }

            return true;
        }
    }

    internal record LinearGradientVar(Point P0, Point P1, Point P2, GradientStopVar[] Stops, GradientSpreadMethod Extend, uint VarIndexBase) : Paint
    {
        public static bool TryParse(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context, out Paint? paint)
        {
            return LinearGradient.TryParseInternal(span, paintOffset, in context, isVariable: true, out paint);
        }
    }

    // format 6, 7
    internal record RadialGradient(Point C0, double R0, Point C1, double R1, GradientStop[] Stops, GradientSpreadMethod Extend) : Paint
    {
        public static bool TryParse(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context, out Paint? paint)
        {
            return TryParseInternal(span, paintOffset, in context, isVariable: false, out paint);
        }

        internal static bool TryParseInternal(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context, bool isVariable, out Paint? paint)
        {
            paint = null;

            var minSize = isVariable ? 20 : 16;

            if (span.Length < minSize)
            {
                return false;
            }

            var colorLineOffset = PaintParsingHelpers.ReadOffset24(span.Slice(1));
            var x0 = BinaryPrimitives.ReadInt16BigEndian(span.Slice(4));
            var y0 = BinaryPrimitives.ReadInt16BigEndian(span.Slice(6));
            var r0 = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(8));
            var x1 = BinaryPrimitives.ReadInt16BigEndian(span.Slice(10));
            var y1 = BinaryPrimitives.ReadInt16BigEndian(span.Slice(12));
            var r1 = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(14));

            var colorLineAbsOffset = paintOffset + colorLineOffset;

            if (!PaintParsingHelpers.TryParseColorLine(context.ColrData.Span, colorLineAbsOffset, in context, isVarColorLine: isVariable,
                out var immutableStops,
                out var extend))
            {
                return false;
            }

            if (isVariable)
            {
                var varIndexBase = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(16));
                var stops = new GradientStopVar[immutableStops.Length];

                for (int i = 0; i < immutableStops.Length; i++)
                {
                    stops[i] = new GradientStopVar(immutableStops[i].Offset, immutableStops[i].Color, 0);
                }

                paint = new RadialGradientVar(new Point(x0, y0), r0, new Point(x1, y1), r1, stops, extend, varIndexBase);
            }
            else
            {
                var stops = new GradientStop[immutableStops.Length];

                for (int i = 0; i < immutableStops.Length; i++)
                {
                    stops[i] = new GradientStop(immutableStops[i].Offset, immutableStops[i].Color);
                }

                paint = new RadialGradient(new Point(x0, y0), r0, new Point(x1, y1), r1, stops, extend);
            }

            return true;
        }
    }

    internal record RadialGradientVar(Point C0, double R0, Point C1, double R1, GradientStopVar[] Stops, GradientSpreadMethod Extend, uint VarIndexBase) : Paint
    {
        public static bool TryParse(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context, out Paint? paint)
        {
            return RadialGradient.TryParseInternal(span, paintOffset, in context, isVariable: true, out paint);
        }
    }

    // format 8, 9
    internal record SweepGradient(Point Center, double StartAngle, double EndAngle, GradientStop[] Stops, GradientSpreadMethod Extend) : Paint
    {
        public static bool TryParse(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context, out Paint? paint)
        {
            return TryParseInternal(span, paintOffset, in context, isVariable: false, out paint);
        }

        internal static bool TryParseInternal(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context, bool isVariable, out Paint? paint)
        {
            paint = null;

            var minSize = isVariable ? 16 : 12;

            if (span.Length < minSize)
            {
                return false;
            }

            var colorLineOffset = PaintParsingHelpers.ReadOffset24(span.Slice(1));
            var centerX = BinaryPrimitives.ReadInt16BigEndian(span.Slice(4));
            var centerY = BinaryPrimitives.ReadInt16BigEndian(span.Slice(6));
            var startAngleFixed = BinaryPrimitives.ReadInt16BigEndian(span.Slice(8));
            var endAngleFixed = BinaryPrimitives.ReadInt16BigEndian(span.Slice(10));

            // F2DOT14 angles: 180° per 1.0 of value, so multiply by π (not 2π)
            var startAngle = PaintParsingHelpers.F2Dot14ToDouble(startAngleFixed) * Math.PI;
            var endAngle = PaintParsingHelpers.F2Dot14ToDouble(endAngleFixed) * Math.PI;

            var colorLineAbsOffset = paintOffset + colorLineOffset;

            if (!PaintParsingHelpers.TryParseColorLine(
                context.ColrData.Span,
                colorLineAbsOffset,
                in context,
                isVarColorLine: isVariable,
                out var immutableStops,
                out var extend))
            {
                return false;
            }

            if (isVariable)
            {
                var varIndexBase = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(12));
                var stops = new GradientStopVar[immutableStops.Length];

                for (int i = 0; i < immutableStops.Length; i++)
                {
                    stops[i] = new GradientStopVar(immutableStops[i].Offset, immutableStops[i].Color, 0);
                }

                paint = new SweepGradientVar(new Point(centerX, centerY), startAngle, endAngle, stops, extend, varIndexBase);
            }
            else
            {
                var stops = new GradientStop[immutableStops.Length];

                for (int i = 0; i < immutableStops.Length; i++)
                {
                    stops[i] = new GradientStop(immutableStops[i].Offset, immutableStops[i].Color);
                }

                paint = new SweepGradient(new Point(centerX, centerY), startAngle, endAngle, stops, extend);
            }

            return true;
        }
    }

    internal record SweepGradientVar(Point Center, double StartAngle, double EndAngle, GradientStopVar[] Stops, GradientSpreadMethod Extend, uint VarIndexBase) : Paint
    {
        public static bool TryParse(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context, out Paint? paint)
        {
            return SweepGradient.TryParseInternal(span, paintOffset, in context, isVariable: true, out paint);
        }
    }

    // format 10
    internal record Glyph(ushort GlyphId, Paint Paint) : Paint
    {
        public static bool TryParse(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context, in PaintDecycler decycler, out Paint? paint)
        {
            paint = null;

            if (span.Length < 6)
            {
                return false;
            }

            var subPaintOffset = paintOffset + PaintParsingHelpers.ReadOffset24(span.Slice(1));
            var glyphId = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(4));

            decycler.Enter(glyphId);

            if (subPaintOffset >= context.ColrData.Length)
            {
                return false;
            }

            if (!PaintParser.TryParse(context.ColrData.Span, subPaintOffset, in context, in decycler, out var innerPaint))
            {
                return false;
            }

            decycler.Exit(glyphId);

            paint = new Glyph(glyphId, innerPaint);

            return true;
        }
    }

    // format 11
    internal record ColrGlyph(ushort GlyphId, Paint Inner) : Paint
    {
        public static bool TryParse(ReadOnlySpan<byte> span, in ColrContext context, in PaintDecycler decycler, out Paint? paint)
        {
            paint = null;

            if (span.Length < 3)
            {
                return false;
            }

            var glyphId = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(1));

            decycler.Enter(glyphId);

            if (!context.ColrTable.TryGetBaseGlyphV1Record((ushort)glyphId, out var v1Record))
            {
                return false;
            }

            var absolutePaintOffset = context.ColrTable.GetAbsolutePaintOffset(v1Record.PaintOffset);

            if (absolutePaintOffset >= context.ColrData.Length)
            {
                return false;
            }

            if (!PaintParser.TryParse(context.ColrData.Span, absolutePaintOffset, in context, in decycler, out var innerPaint))
            {
                return false;
            }

            decycler.Exit(glyphId);

            paint = new ColrGlyph(glyphId, innerPaint);

            return true;
        }
    }

    // format 12 and 13
    internal record Transform(Paint Inner, Matrix Matrix) : Paint
    {
        public static bool TryParse(ReadOnlySpan<byte> span, byte format, uint paintOffset, in ColrContext context,
            in PaintDecycler decycler, out Paint? paint)
        {
            var isVariable = format == 13;

            return TryParseInternal(span, paintOffset, in context, in decycler, isVariable, out paint);
        }

        internal static bool TryParseInternal(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context,
            in PaintDecycler decycler, bool isVariable, out Paint? paint)
        {
            paint = null;

            var minSize = isVariable ? 12 : 8; // 8 for fixed, 12 for variable (+ 4 bytes for varIndexBase)

            if (span.Length < minSize)
            {
                return false;
            }

            var subPaintOffset = paintOffset + PaintParsingHelpers.ReadOffset24(span.Slice(1));

            if (subPaintOffset >= context.ColrData.Length)
            {
                return false;
            }

            if (!PaintParser.TryParse(context.ColrData.Span, subPaintOffset, in context, in decycler, out var sourcePaint))
            {
                return false;
            }

            var transform = PaintParsingHelpers.ParseAffine2x3(span);

            if (isVariable)
            {
                var varIndexBase = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(minSize - 4));

                paint = new TransformVar(sourcePaint, transform, varIndexBase);
            }
            else
            {
                paint = new Transform(sourcePaint, transform);
            }

            return true;
        }
    }

    internal record TransformVar(Paint Inner, Matrix Matrix, uint VarIndexBase) : Paint
    {
        public static bool TryParse(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context, in PaintDecycler decycler, out Paint? paint)
        {
            return Transform.TryParseInternal(span, paintOffset, in context, in decycler, isVariable: true, out paint);
        }
    }

    // format 14 and 15
    internal record Translate(Paint Inner, double Dx, double Dy) : Paint
    {
        public static bool TryParse(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context, in PaintDecycler decycler, out Paint? paint)
        {
            return TryParseInternal(span, paintOffset, in context, in decycler, isVariable: false, out paint);
        }

        internal static bool TryParseInternal(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context,
            in PaintDecycler decycler, bool isVariable, out Paint? paint)
        {
            paint = null;

            var minSize = isVariable ? 12 : 8;

            if (span.Length < minSize)
            {
                return false;
            }

            var subPaintOffset = paintOffset + PaintParsingHelpers.ReadOffset24(span.Slice(1));

            if (subPaintOffset >= context.ColrData.Length)
            {
                return false;
            }

            if (!PaintParser.TryParse(context.ColrData.Span, subPaintOffset, in context, in decycler, out var sourcePaint))
            {
                return false;
            }

            var dx = BinaryPrimitives.ReadInt16BigEndian(span.Slice(4));
            var dy = BinaryPrimitives.ReadInt16BigEndian(span.Slice(6));

            if (isVariable)
            {
                var varIndexBase = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(8));

                paint = new TranslateVar(sourcePaint, dx, dy, varIndexBase);
            }
            else
            {
                paint = new Translate(sourcePaint, dx, dy);
            }

            return true;
        }
    }

    internal record TranslateVar(Paint Inner, double Dx, double Dy, uint VarIndexBase) : Paint
    {
        public static bool TryParse(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context, in PaintDecycler decycler, out Paint? paint)
        {
            return Translate.TryParseInternal(span, paintOffset, in context, in decycler, isVariable: true, out paint);
        }
    }

    // format 16 and 17
    internal record Scale(Paint Inner, double Sx, double Sy) : Paint
    {
        public static bool TryParse(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context, in PaintDecycler decycler, out Paint? paint)
        {
            return TryParseInternal(span, paintOffset, in context, in decycler, isVariable: false, out paint);
        }

        internal static bool TryParseInternal(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context,
            in PaintDecycler decycler, bool isVariable, out Paint? paint)
        {
            paint = null;

            var minSize = isVariable ? 12 : 8;

            if (span.Length < minSize)
            {
                return false;
            }

            var subPaintOffset = paintOffset + PaintParsingHelpers.ReadOffset24(span.Slice(1));

            if (subPaintOffset >= context.ColrData.Length)
            {
                return false;
            }

            if (!PaintParser.TryParse(context.ColrData.Span, subPaintOffset, in context, in decycler, out var sourcePaint))
            {
                return false;
            }

            var scaleX = PaintParsingHelpers.F2Dot14ToDouble(BinaryPrimitives.ReadInt16BigEndian(span.Slice(4)));
            var scaleY = PaintParsingHelpers.F2Dot14ToDouble(BinaryPrimitives.ReadInt16BigEndian(span.Slice(6)));

            if (isVariable)
            {
                var varIndexBase = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(8));

                paint = new ScaleVar(sourcePaint, scaleX, scaleY, varIndexBase);
            }
            else
            {
                paint = new Scale(sourcePaint, scaleX, scaleY);
            }

            return true;
        }
    }

    internal record ScaleVar(Paint Inner, double Sx, double Sy, uint VarIndexBase) : Paint
    {
        public static bool TryParse(ReadOnlySpan<byte> span, uint paintOffset,
            in ColrContext context, in PaintDecycler decycler, out Paint? paint)
        {
            return Scale.TryParseInternal(span, paintOffset, in context, in decycler, isVariable: true, out paint);
        }
    }

    // format 18 and 19
    internal record ScaleAroundCenter(Paint Inner, double Sx, double Sy, Point Center) : Paint
    {
        public static bool TryParse(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context,
            in PaintDecycler decycler, out Paint? paint)
        {
            return TryParseInternal(span, paintOffset, in context, in decycler, isVariable: false, out paint);
        }

        internal static bool TryParseInternal(ReadOnlySpan<byte> span, uint paintOffset,
            in ColrContext context, in PaintDecycler decycler, bool isVariable, out Paint? paint)
        {
            paint = null;

            var minSize = isVariable ? 16 : 12;

            if (span.Length < minSize)
            {
                return false;
            }

            var subPaintOffset = paintOffset + PaintParsingHelpers.ReadOffset24(span.Slice(1));

            if (subPaintOffset >= context.ColrData.Length)
            {
                return false;
            }

            if (!PaintParser.TryParse(context.ColrData.Span, subPaintOffset, in context, in decycler, out var sourcePaint))
            {
                return false;
            }

            var scaleX = PaintParsingHelpers.F2Dot14ToDouble(BinaryPrimitives.ReadInt16BigEndian(span.Slice(4)));
            var scaleY = PaintParsingHelpers.F2Dot14ToDouble(BinaryPrimitives.ReadInt16BigEndian(span.Slice(6)));
            var centerX = BinaryPrimitives.ReadInt16BigEndian(span.Slice(8));
            var centerY = BinaryPrimitives.ReadInt16BigEndian(span.Slice(10));

            if (isVariable)
            {
                var varIndexBase = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(12));

                paint = new ScaleAroundCenterVar(sourcePaint, scaleX, scaleY, new Point(centerX, centerY), varIndexBase);
            }
            else
            {
                paint = new ScaleAroundCenter(sourcePaint, scaleX, scaleY, new Point(centerX, centerY));
            }

            return true;
        }
    }

    internal record ScaleAroundCenterVar(Paint Inner, double Sx, double Sy, Point Center, uint VarIndexBase) : Paint
    {
        public static bool TryParse(ReadOnlySpan<byte> span, uint paintOffset,
            in ColrContext context, in PaintDecycler decycler, out Paint? paint)
        {
            return ScaleAroundCenter.TryParseInternal(span, paintOffset, in context, in decycler, isVariable: true, out paint);
        }
    }

    // format 20 and 21
    internal record ScaleUniform(Paint Inner, double Scale) : Paint
    {
        public static bool TryParse(ReadOnlySpan<byte> span, uint paintOffset,
            in ColrContext context, in PaintDecycler decycler, out Paint? paint)
        {
            return TryParseInternal(span, paintOffset, in context, in decycler, isVariable: false, out paint);
        }

        internal static bool TryParseInternal(ReadOnlySpan<byte> span, uint paintOffset,
            in ColrContext context, in PaintDecycler decycler, bool isVariable, out Paint? paint)
        {
            paint = null;

            var minSize = isVariable ? 10 : 6;

            if (span.Length < minSize)
            {
                return false;
            }

            var subPaintOffset = paintOffset + PaintParsingHelpers.ReadOffset24(span.Slice(1));

            if (subPaintOffset >= context.ColrData.Length)
            {
                return false;
            }

            if (!PaintParser.TryParse(context.ColrData.Span, subPaintOffset, in context, in decycler, out var sourcePaint))
            {
                return false;
            }

            var scale = PaintParsingHelpers.F2Dot14ToDouble(BinaryPrimitives.ReadInt16BigEndian(span.Slice(4)));

            if (isVariable)
            {
                var varIndexBase = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(6));

                paint = new ScaleUniformVar(sourcePaint, scale, varIndexBase);
            }
            else
            {
                paint = new ScaleUniform(sourcePaint, scale);
            }

            return true;
        }
    }

    internal record ScaleUniformVar(Paint Inner, double Scale, uint VarIndexBase) : Paint
    {
        public static bool TryParse(ReadOnlySpan<byte> span, uint paintOffset,
            in ColrContext context, in PaintDecycler decycler, out Paint? paint)
        {
            return ScaleUniform.TryParseInternal(span, paintOffset, in context, in decycler, isVariable: true, out paint);
        }
    }

    // format 22 and 23
    internal record ScaleUniformAroundCenter(Paint Inner, double Scale, Point Center) : Paint
    {
        public static bool TryParse(ReadOnlySpan<byte> span, uint paintOffset,
            in ColrContext context, in PaintDecycler decycler, out Paint? paint)
        {
            return TryParseInternal(span, paintOffset, in context, in decycler, isVariable: false, out paint);
        }

        internal static bool TryParseInternal(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context,
            in PaintDecycler decycler, bool isVariable, out Paint? paint)
        {
            paint = null;

            var minSize = isVariable ? 14 : 10;

            if (span.Length < minSize)
            {
                return false;
            }

            var subPaintOffset = paintOffset + PaintParsingHelpers.ReadOffset24(span.Slice(1));

            if (subPaintOffset >= context.ColrData.Length)
            {
                return false;
            }

            if (!PaintParser.TryParse(context.ColrData.Span, subPaintOffset, in context, in decycler, out var sourcePaint))
            {
                return false;
            }

            var scale = PaintParsingHelpers.F2Dot14ToDouble(BinaryPrimitives.ReadInt16BigEndian(span.Slice(4)));
            var centerX = BinaryPrimitives.ReadInt16BigEndian(span.Slice(6));
            var centerY = BinaryPrimitives.ReadInt16BigEndian(span.Slice(8));

            if (isVariable)
            {
                var varIndexBase = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(10));

                paint = new ScaleUniformAroundCenterVar(sourcePaint, scale, new Point(centerX, centerY), varIndexBase);
            }
            else
            {
                paint = new ScaleUniformAroundCenter(sourcePaint, scale, new Point(centerX, centerY));
            }

            return true;
        }
    }

    internal record ScaleUniformAroundCenterVar(Paint Inner, double Scale, Point Center, uint VarIndexBase) : Paint
    {
        public static bool TryParse(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context,
            in PaintDecycler decycler, out Paint? paint)
        {
            return ScaleUniformAroundCenter.TryParseInternal(span, paintOffset, in context, in decycler, isVariable: true, out paint);
        }
    }

    // format 24 and 25
    internal record Rotate(Paint Inner, double Angle) : Paint
    {
        public static bool TryParse(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context,
            in PaintDecycler decycler, out Paint? paint)
        {
            return TryParseInternal(span, paintOffset, in context, in decycler, isVariable: false, out paint);
        }

        internal static bool TryParseInternal(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context,
            in PaintDecycler decycler, bool isVariable, out Paint? paint)
        {
            paint = null;

            var minSize = isVariable ? 10 : 6;

            if (span.Length < minSize)
            {
                return false;
            }

            var subPaintOffset = paintOffset + PaintParsingHelpers.ReadOffset24(span.Slice(1));

            if (subPaintOffset >= context.ColrData.Length)
            {
                return false;
            }

            if (!PaintParser.TryParse(context.ColrData.Span, subPaintOffset, in context, in decycler, out var sourcePaint))
            {
                return false;
            }

            // F2DOT14 angles: 180° per 1.0 of value, so multiply by π (not 2π)
            var angle = PaintParsingHelpers.F2Dot14ToDouble(BinaryPrimitives.ReadInt16BigEndian(span.Slice(4))) * Math.PI;

            if (isVariable)
            {
                var varIndexBase = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(6));

                paint = new RotateVar(sourcePaint, angle, varIndexBase);
            }
            else
            {
                paint = new Rotate(sourcePaint, angle);
            }

            return true;
        }
    }

    internal record RotateVar(Paint Inner, double Angle, uint VarIndexBase) : Paint
    {
        public static bool TryParse(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context, in PaintDecycler decycler, out Paint? paint)
        {
            return Rotate.TryParseInternal(span, paintOffset, in context, in decycler, isVariable: true, out paint);
        }
    }

    // format 26 and 27
    internal record RotateAroundCenter(Paint Inner, double Angle, Point Center) : Paint
    {
        public static bool TryParse(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context, in PaintDecycler decycler, out Paint? paint)
        {
            return TryParseInternal(span, paintOffset, in context, in decycler, isVariable: false, out paint);
        }

        internal static bool TryParseInternal(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context,
            in PaintDecycler decycler, bool isVariable, out Paint? paint)
        {
            paint = null;

            var minSize = isVariable ? 14 : 10;

            if (span.Length < minSize)
            {
                return false;
            }

            var subPaintOffset = paintOffset + PaintParsingHelpers.ReadOffset24(span.Slice(1));

            if (subPaintOffset >= context.ColrData.Length)
            {
                return false;
            }

            if (!PaintParser.TryParse(context.ColrData.Span, subPaintOffset, in context, in decycler, out var sourcePaint))
            {
                return false;
            }

            // F2DOT14 angles: 180° per 1.0 of value, so multiply by π (not 2π)
            var angle = PaintParsingHelpers.F2Dot14ToDouble(BinaryPrimitives.ReadInt16BigEndian(span.Slice(4))) * Math.PI;
            var centerX = BinaryPrimitives.ReadInt16BigEndian(span.Slice(6));
            var centerY = BinaryPrimitives.ReadInt16BigEndian(span.Slice(8));

            if (isVariable)
            {
                var varIndexBase = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(10));

                paint = new RotateAroundCenterVar(sourcePaint, angle, new Point(centerX, centerY), varIndexBase);
            }
            else
            {
                paint = new RotateAroundCenter(sourcePaint, angle, new Point(centerX, centerY));
            }

            return true;
        }
    }

    internal record RotateAroundCenterVar(Paint Inner, double Angle, Point Center, uint VarIndexBase) : Paint
    {
        public static bool TryParse(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context, in PaintDecycler decycler, out Paint? paint)
        {
            return RotateAroundCenter.TryParseInternal(span, paintOffset, in context, in decycler, isVariable: true, out paint);
        }
    }

    // format 28 and 29
    internal record Skew(Paint Inner, double XAngle, double YAngle) : Paint
    {
        public static bool TryParse(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context, in PaintDecycler decycler, out Paint? paint)
        {
            return TryParseInternal(span, paintOffset, in context, in decycler, isVariable: false, out paint);
        }

        internal static bool TryParseInternal(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context,
            in PaintDecycler decycler, bool isVariable, out Paint? paint)
        {
            paint = null;

            var minSize = isVariable ? 12 : 8;

            if (span.Length < minSize)
            {
                return false;
            }

            var subPaintOffset = paintOffset + PaintParsingHelpers.ReadOffset24(span.Slice(1));

            if (subPaintOffset >= context.ColrData.Length)
            {
                return false;
            }

            if (!PaintParser.TryParse(context.ColrData.Span, subPaintOffset, in context, in decycler, out var sourcePaint))
            {
                return false;
            }

            // F2DOT14 angles: 180° per 1.0 of value, so multiply by π (not 2π)
            var xAngle = PaintParsingHelpers.F2Dot14ToDouble(BinaryPrimitives.ReadInt16BigEndian(span.Slice(4))) * Math.PI;
            var yAngle = PaintParsingHelpers.F2Dot14ToDouble(BinaryPrimitives.ReadInt16BigEndian(span.Slice(6))) * Math.PI;

            if (isVariable)
            {
                var varIndexBase = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(8));

                paint = new SkewVar(sourcePaint, xAngle, yAngle, varIndexBase);
            }
            else
            {
                paint = new Skew(sourcePaint, xAngle, yAngle);
            }

            return true;
        }
    }

    internal record SkewVar(Paint Inner, double XAngle, double YAngle, uint VarIndexBase) : Paint
    {
        public static bool TryParse(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context, in PaintDecycler decycler, out Paint? paint)
        {
            return Skew.TryParseInternal(span, paintOffset, in context, in decycler, isVariable: true, out paint);
        }
    }

    // format 30 and 31
    internal record SkewAroundCenter(Paint Inner, double XAngle, double YAngle, Point Center) : Paint
    {
        public static bool TryParse(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context, in PaintDecycler decycler, out Paint? paint)
        {
            return TryParseInternal(span, paintOffset, in context, in decycler, isVariable: false, out paint);
        }

        internal static bool TryParseInternal(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context,
            in PaintDecycler decycler, bool isVariable, out Paint? paint)
        {
            paint = null;

            var minSize = isVariable ? 16 : 12;

            if (span.Length < minSize)
            {
                return false;
            }

            var subPaintOffset = paintOffset + PaintParsingHelpers.ReadOffset24(span.Slice(1));

            if (subPaintOffset >= context.ColrData.Length)
            {
                return false;
            }

            if (!PaintParser.TryParse(context.ColrData.Span, subPaintOffset, in context, in decycler, out var sourcePaint))
            {
                return false;
            }

            // F2DOT14 angles: 180° per 1.0 of value, so multiply by π (not 2π)
            var xAngle = PaintParsingHelpers.F2Dot14ToDouble(BinaryPrimitives.ReadInt16BigEndian(span.Slice(4))) * Math.PI;
            var yAngle = PaintParsingHelpers.F2Dot14ToDouble(BinaryPrimitives.ReadInt16BigEndian(span.Slice(6))) * Math.PI;
            var centerX = BinaryPrimitives.ReadInt16BigEndian(span.Slice(8));
            var centerY = BinaryPrimitives.ReadInt16BigEndian(span.Slice(10));

            if (isVariable)
            {
                var varIndexBase = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(12));

                paint = new SkewAroundCenterVar(sourcePaint, xAngle, yAngle, new Point(centerX, centerY), varIndexBase);
            }
            else
            {
                paint = new SkewAroundCenter(sourcePaint, xAngle, yAngle, new Point(centerX, centerY));
            }

            return true;
        }
    }

    internal record SkewAroundCenterVar(Paint Inner, double XAngle, double YAngle, Point Center, uint VarIndexBase) : Paint
    {
        public static bool TryParse(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context, in PaintDecycler decycler, out Paint? paint)
        {
            return SkewAroundCenter.TryParseInternal(span, paintOffset, in context, in decycler, isVariable: true, out paint);
        }
    }

    // format 32
    internal record Composite(Paint Backdrop, Paint Source, CompositeMode Mode) : Paint
    {
        public static bool TryParse(ReadOnlySpan<byte> span, uint paintOffset, in ColrContext context, in PaintDecycler decycler, out Paint? paint)
        {
            paint = null;

            if (span.Length < 8)
            {
                return false;
            }

            var sourcePaintOffset = paintOffset + PaintParsingHelpers.ReadOffset24(span.Slice(1));
            var mode = (CompositeMode)span[4];
            var backdropPaintOffset = paintOffset + PaintParsingHelpers.ReadOffset24(span.Slice(5));

            if (sourcePaintOffset >= context.ColrData.Length || backdropPaintOffset >= context.ColrData.Length)
            {
                return false;
            }

            if (!PaintParser.TryParse(context.ColrData.Span, sourcePaintOffset, in context, in decycler, out var sourcePaint))
            {
                return false;
            }

            if (!PaintParser.TryParse(context.ColrData.Span, backdropPaintOffset, in context, in decycler, out var backdropPaint))
            {
                return false;
            }

            paint = new Composite(backdropPaint, sourcePaint, mode);

            return true;
        }
    }
}
