using System;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Media.Fonts.Tables.Colr
{
    internal static class PaintParser
    {
        /// <summary>
        /// Tries to parse a Paint from the given data at the specified offset.
        /// </summary>
        public static bool TryParse(ReadOnlySpan<byte> data, uint offset, in ColrContext context, in PaintDecycler decycler, [NotNullWhen(true)] out Paint? paint)
        {
            paint = null;

            if (offset >= data.Length || data.Length - offset < 1)
            {
                return false;
            }

            var span = data.Slice((int)offset);

            var format = span[0];

            if (format > 32)
            {
                return false;
            }

            return format switch
            {
                1 => ColrLayers.TryParse(span, offset, in context, in decycler, out paint),
                2 => Solid.TryParse(span, in context, out paint),
                3 => SolidVar.TryParse(span, in context, out paint),
                4 => LinearGradient.TryParse(span, offset, in context, out paint),
                5 => LinearGradientVar.TryParse(span, offset, in context, out paint),
                6 => RadialGradient.TryParse(span, offset, in context, out paint),
                7 => RadialGradientVar.TryParse(span, offset, in context, out paint),
                8 => SweepGradient.TryParse(span, offset, in context, out paint),
                9 => SweepGradientVar.TryParse(span, offset, in context, out paint),
                10 => Glyph.TryParse(span, offset, in context, in decycler, out paint),
                11 => ColrGlyph.TryParse(span, in context, in decycler, out paint),
                12 or 13 => Transform.TryParse(span, format, offset, in context, in decycler, out paint),
                14 => Translate.TryParse(span, offset, in context, in decycler, out paint),
                15 => TranslateVar.TryParse(span, offset, in context, in decycler, out paint),
                16 => Scale.TryParse(span, offset, in context, in decycler, out paint),
                17 => ScaleVar.TryParse(span, offset, in context, in decycler, out paint),
                18 => ScaleAroundCenter.TryParse(span, offset, in context, in decycler, out paint),
                19 => ScaleAroundCenterVar.TryParse(span, offset, in context, in decycler, out paint),
                20 => ScaleUniform.TryParse(span, offset, in context, in decycler, out paint),
                21 => ScaleUniformVar.TryParse(span, offset, in context, in decycler, out paint),
                22 => ScaleUniformAroundCenter.TryParse(span, offset, in context, in decycler, out paint),
                23 => ScaleUniformAroundCenterVar.TryParse(span, offset, in context, in decycler, out paint),
                24 => Rotate.TryParse(span, offset, in context, in decycler, out paint),
                25 => RotateVar.TryParse(span, offset, in context, in decycler, out paint),
                26 => RotateAroundCenter.TryParse(span, offset, in context, in decycler, out paint),
                27 => RotateAroundCenterVar.TryParse(span, offset, in context, in decycler, out paint),
                28 => Skew.TryParse(span, offset, in context, in decycler, out paint),
                29 => SkewVar.TryParse(span, offset, in context, in decycler, out paint),
                30 => SkewAroundCenter.TryParse(span, offset, in context, in decycler, out paint),
                31 => SkewAroundCenterVar.TryParse(span, offset, in context, in decycler, out paint),
                32 => Composite.TryParse(span, offset, in context, in decycler, out paint),
                _ => false
            };
        }
    }
}
