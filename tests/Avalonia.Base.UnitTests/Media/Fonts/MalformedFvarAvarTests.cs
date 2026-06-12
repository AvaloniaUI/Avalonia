using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts
{
    /// <summary>
    /// Characterization tests for the fvar / avar robustness of the GlyphTypeface parsers, using the
    /// <see cref="SyntheticFont"/> / <see cref="BigEndianBuffer"/> harness. A hostile axis count or a
    /// truncated optional avar must degrade to a static / identity normalization rather than denying
    /// the font or feeding an unbounded stack allocation.
    /// </summary>
    public class MalformedFvarAvarTests
    {
        [Fact]
        public void Truncated_Avar_Segment_Map_Does_Not_Deny_The_Font()
        {
            // The avar header is 6 bytes (major/minor version + axisCount). Keeping 8 leaves the
            // first segment map's positionMapCount readable but cuts its value-pair array (and the
            // next map), so the read loop would over-run AvarTable's reader.
            var font = SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable).Truncate("avar", 8);

            var typeface = font.TryCreateGlyphTypeface();

            // AvarTable.TryLoad now bounds its reads (try/catch), so a malformed optional avar degrades
            // to identity normalization instead of denying the whole font.
            Assert.NotNull(typeface);
        }

        [Fact]
        public void FvarTable_Rejects_An_Unclamped_AxisCount()
        {
            // No real font exceeds ~64 variation axes. An unclamped axisCount flows into three
            // `stackalloc float[axisCount]` buffers in GlyphVariationReader — an uncatchable
            // StackOverflow (DoS) on a hostile variable font declaring thousands of axes.
            const ushort axisCount = 2048;
            var font = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular)
                .Replace("fvar", BuildFvar(axisCount));

            var typeface = font.TryCreateGlyphTypeface();

            // fvar now rejects an implausibly large axisCount (> MaxAxisCount), so the font loads as
            // static (no variation axes) rather than feeding the gvar stackalloc.
            Assert.NotNull(typeface);
            Assert.Empty(typeface!.VariationAxes);
        }

        /// <summary>Builds a valid fvar table declaring <paramref name="axisCount"/> axes (no instances).</summary>
        private static byte[] BuildFvar(ushort axisCount)
        {
            var fvar = new BigEndianBuffer();

            // fvar header (16 bytes).
            fvar.UInt16(1);          // majorVersion
            fvar.UInt16(0);          // minorVersion
            fvar.UInt16(16);         // axesArrayOffset (axes follow the header)
            fvar.UInt16(2);          // reserved
            fvar.UInt16(axisCount);
            fvar.UInt16(20);         // axisSize
            fvar.UInt16(0);          // instanceCount
            fvar.UInt16(0);          // instanceSize

            // AxisRecord[axisCount]: Tag(4) + min/default/max Fixed(4 each) + flags(2) + axisNameID(2).
            for (var i = 0; i < axisCount; i++)
            {
                fvar.Tag("axis");
                fvar.Fixed(0.0);     // min
                fvar.Fixed(0.0);     // default
                fvar.Fixed(1.0);     // max
                fvar.UInt16(0);      // flags
                fvar.UInt16(0);      // axisNameID
            }

            return fvar.ToArray();
        }
    }
}
