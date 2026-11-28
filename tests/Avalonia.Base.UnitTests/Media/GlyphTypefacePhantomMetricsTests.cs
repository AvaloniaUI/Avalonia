using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    /// <summary>
    /// Glyph advances of variable TrueType fonts that ship no HVAR / VVAR table, where the
    /// varied advance comes from the gvar deltas of the glyph's phantom points.
    /// </summary>
    public class GlyphTypefacePhantomMetricsTests
    {
        private const string AdobeBlankAsset =
            "resm:Avalonia.Base.UnitTests.Assets.AdobeBlank2VF.ttf?assembly=Avalonia.Base.UnitTests";

        private static readonly OpenTypeTag s_wght = OpenTypeTag.Parse("wght");
        private static readonly OpenTypeTag s_hght = OpenTypeTag.Parse("HGHT");

        // InterVariable glyph indices.
        private const ushort InterA = 2;
        private const ushort InterAacute = 6;
        private const ushort InterBStroke = 69;           // uni0243, USE_MY_METRICS on B
        private const ushort InterDasiaOxiaTonos = 2909;  // uni1FEF.tonos, USE_MY_METRICS on uni1FEF

        // Expected advances are hmtx plus the pp2.x - pp1.x phantom delta at the given weight
        // (opsz at its default), computed with fontTools from the font's gvar after avar
        // normalization and rounded to font units.
        [Theory]
        [InlineData('A', 900, 1628)]
        [InlineData('a', 900, 1222)]
        [InlineData('H', 900, 1536)]
        [InlineData(' ', 900, 408)]
        [InlineData('A', 100, 1310)]
        [InlineData('a', 100, 1076)]
        [InlineData('A', 650, 1510)]
        [InlineData('H', 650, 1528)]
        [InlineData(' ', 650, 500)]
        public void Horizontal_Advance_Without_Hvar_Follows_Gvar_Phantom_Points(char character, double weight, int expected)
        {
            var typeface = InterWithoutHvar();
            var glyph = typeface.CharacterToGlyphMap[character];
            var varied = AtWeight(typeface, weight);

            Assert.True(varied.TryGetHorizontalGlyphAdvance(glyph, out var single));
            Assert.Equal(expected, single);

            var batch = new ushort[1];
            Assert.True(varied.TryGetHorizontalGlyphAdvances(new[] { glyph }, batch));
            Assert.Equal(expected, batch[0]);

            Assert.True(varied.TryGetGlyphMetrics(glyph, out var metrics));
            Assert.Equal(expected, metrics.AdvanceWidth);

            var batchMetrics = new GlyphMetrics[1];
            Assert.True(varied.TryGetGlyphMetrics(new[] { glyph }, batchMetrics));
            Assert.Equal(expected, batchMetrics[0].AdvanceWidth);
        }

        [Fact]
        public void Horizontal_Advance_Without_Hvar_Matches_The_Hvar_Advance_For_Every_Glyph()
        {
            // Inter's HVAR and the phantom points in its gvar are built from the same masters,
            // so both paths agree on every glyph except two composites whose USE_MY_METRICS
            // component is narrower at this weight than the composite's own masters: HVAR
            // follows the masters, the phantom points follow the flag. The flagged values are
            // what FreeType and HarfBuzz report for the font without HVAR.
            var withHvar = AtWeight(new GlyphTypeface(SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable)
                .ToPlatformTypeface()), 900);
            var withoutHvar = AtWeight(InterWithoutHvar(), 900);

            var glyphs = new ushort[withHvar.GlyphCount];
            for (var i = 0; i < glyphs.Length; i++)
            {
                glyphs[i] = (ushort)i;
            }

            var expected = new ushort[glyphs.Length];
            var actual = new ushort[glyphs.Length];

            Assert.True(withHvar.TryGetHorizontalGlyphAdvances(glyphs, expected));
            Assert.True(withoutHvar.TryGetHorizontalGlyphAdvances(glyphs, actual));

            Assert.Equal(1448, expected[InterBStroke]);
            Assert.Equal(652, expected[InterDasiaOxiaTonos]);
            expected[InterBStroke] = 1368;
            expected[InterDasiaOxiaTonos] = 448;

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Composite_With_Use_My_Metrics_Takes_The_Advance_Of_Its_Component()
        {
            // Aacute is A plus an accent, with USE_MY_METRICS on A. With its own gvar entry
            // emptied, the composite's own phantom points no longer move; the flag still makes
            // it take A's varied phantom points, so both advance alike.
            var font = SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable)
                .Remove("HVAR")
                .Mutate("gvar", gvar => EmptyGvarEntry(gvar, InterAacute));

            var varied = AtWeight(new GlyphTypeface(font.ToPlatformTypeface()), 900);

            Assert.True(varied.TryGetHorizontalGlyphAdvance(InterA, out var baseAdvance));
            Assert.Equal(1628, baseAdvance);

            Assert.True(varied.TryGetHorizontalGlyphAdvance(InterAacute, out var compositeAdvance));
            Assert.Equal(baseAdvance, compositeAdvance);

            var batch = new ushort[1];
            Assert.True(varied.TryGetHorizontalGlyphAdvances(new[] { InterAacute }, batch));
            Assert.Equal(baseAdvance, batch[0]);

            Assert.True(varied.TryGetGlyphMetrics(InterAacute, out var metrics));
            Assert.Equal(baseAdvance, metrics.AdvanceWidth);
        }

        [Fact]
        public void Vertical_Advance_Without_Vvar_Follows_Gvar_Phantom_Points()
        {
            // AdobeBlank2VF's glyph 1 is empty, so its gvar entry holds only the four phantom
            // points. Replace its gvar with one whose HGHT tuple moves pp2 right by 700 and pp3
            // up by 900, and drop HVAR / VVAR so the phantom points are the only source of
            // variation. hmtx and vmtx both give glyph 1 a zero advance.
            var font = SyntheticFont.FromAsset(AdobeBlankAsset)
                .Remove("HVAR")
                .Remove("VVAR")
                .Replace("gvar", BuildPhantomOnlyGvar(
                    axisCount: 2, glyphCount: 2, glyph: 1, peak: new short[] { 0, 0x4000 },
                    x: new short[] { 0, 700, 0, 0 }, y: new short[] { 0, 0, 900, 0 }));

            var typeface = new GlyphTypeface(font.ToPlatformTypeface());
            var varied = typeface.WithVariation(typeface.CreateNormalizedPosition(new FontVariationSettings(
                new[] { new FontVariation(s_hght, 1000) })));

            Assert.True(varied.TryGetVerticalGlyphAdvance(1, out var single));
            Assert.Equal(900, single);

            var batch = new ushort[1];
            Assert.True(varied.TryGetVerticalGlyphAdvances(new ushort[] { 1 }, batch));
            Assert.Equal(900, batch[0]);

            Assert.True(varied.TryGetGlyphMetrics(1, out var metrics));
            Assert.Equal(900, metrics.AdvanceHeight);
            Assert.Equal(700, metrics.AdvanceWidth);

            var batchMetrics = new GlyphMetrics[1];
            Assert.True(varied.TryGetGlyphMetrics(new ushort[] { 1 }, batchMetrics));
            Assert.Equal(900, batchMetrics[0].AdvanceHeight);
            Assert.Equal(700, batchMetrics[0].AdvanceWidth);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Truncated_Composite_Glyph_Has_No_Outline_And_Plain_Hmtx_Metrics(bool varied)
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var font = SyntheticFont.FromAsset(varied ? SyntheticFont.Assets.InterVariable : SyntheticFont.Assets.InterRegular)
                .Remove("HVAR");
            var original = new GlyphTypeface(font.ToPlatformTypeface());

            font = font.WithLastGlyphData(PhantomPointFixtures.TruncatedCompositeGlyph(InterA), out var glyph);

            var typeface = new GlyphTypeface(font.ToPlatformTypeface());

            if (varied)
            {
                typeface = AtWeight(typeface, 900);
            }

            AssertPlainHmtxAdvance(original, typeface, glyph);
        }

        [Fact]
        public void Use_My_Metrics_Cycle_Falls_Back_To_The_Glyph_Own_Hmtx_Advance()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var font = SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable).Remove("HVAR");
            var original = new GlyphTypeface(font.ToPlatformTypeface());
            var last = (ushort)(original.GlyphCount - 1);

            // The last glyph takes its metrics from itself, so the chain never ends.
            font = font.WithLastGlyphData(PhantomPointFixtures.UseMyMetricsCompositeGlyph(last), out var glyph);

            AssertPlainHmtxAdvance(original, AtWeight(new GlyphTypeface(font.ToPlatformTypeface()), 900), glyph);
        }

        private static void AssertPlainHmtxAdvance(GlyphTypeface original, GlyphTypeface typeface, ushort glyph)
        {
            Assert.True(original.TryGetHorizontalGlyphAdvance(glyph, out var expected));

            Assert.Null(typeface.GetGlyphOutline(glyph));

            Assert.True(typeface.TryGetHorizontalGlyphAdvance(glyph, out var single));
            Assert.Equal(expected, single);

            var batch = new ushort[1];
            Assert.True(typeface.TryGetHorizontalGlyphAdvances(new[] { glyph }, batch));
            Assert.Equal(expected, batch[0]);

            Assert.True(typeface.TryGetGlyphMetrics(glyph, out var metrics));
            Assert.Equal(expected, metrics.AdvanceWidth);

            var batchMetrics = new GlyphMetrics[1];
            Assert.True(typeface.TryGetGlyphMetrics(new[] { glyph }, batchMetrics));
            Assert.Equal(expected, batchMetrics[0].AdvanceWidth);
        }

        private static GlyphTypeface InterWithoutHvar()
            => new GlyphTypeface(SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable)
                .Remove("HVAR")
                .ToPlatformTypeface());

        private static GlyphTypeface AtWeight(GlyphTypeface typeface, double weight)
            => typeface.WithVariation(typeface.CreateNormalizedPosition(new FontVariationSettings(
                new[] { new FontVariation(s_wght, weight) })));

        /// <summary>
        /// Gives <paramref name="glyph"/> an empty gvar entry by moving its start offset onto its
        /// end. The previous glyph's entry absorbs the bytes, which it never reads because its
        /// own headers say how much data it has.
        /// </summary>
        private static void EmptyGvarEntry(byte[] gvar, int glyph)
        {
            const int offsetsStart = 20;
            var longOffsets = (BinaryPrimitives.ReadUInt16BigEndian(gvar.AsSpan(14)) & 1) != 0;

            if (longOffsets)
            {
                var end = BinaryPrimitives.ReadUInt32BigEndian(gvar.AsSpan(offsetsStart + (glyph + 1) * 4));
                BinaryPrimitives.WriteUInt32BigEndian(gvar.AsSpan(offsetsStart + glyph * 4), end);
            }
            else
            {
                var end = BinaryPrimitives.ReadUInt16BigEndian(gvar.AsSpan(offsetsStart + (glyph + 1) * 2));
                BinaryPrimitives.WriteUInt16BigEndian(gvar.AsSpan(offsetsStart + glyph * 2), end);
            }
        }

        /// <summary>
        /// Builds a gvar table in which only <paramref name="glyph"/> has an entry: one tuple with
        /// an embedded peak that applies to all points of a glyph without contour points, i.e.
        /// to its four phantom points.
        /// </summary>
        private static byte[] BuildPhantomOnlyGvar(
            int axisCount, int glyphCount, int glyph, short[] peak, short[] x, short[] y)
        {
            var serialized = new List<byte>();
            WritePackedWordDeltas(serialized, x);
            WritePackedWordDeltas(serialized, y);

            var entry = new List<byte>();
            WriteU16(entry, 1);                                     // tupleVariationCount, no shared points
            WriteU16(entry, (ushort)(8 + axisCount * 2));           // offset to serialized data
            WriteU16(entry, (ushort)serialized.Count);              // variationDataSize
            WriteU16(entry, 0x8000);                                // tupleIndex: embedded peak
            foreach (var coordinate in peak)
            {
                WriteU16(entry, (ushort)coordinate);
            }
            entry.AddRange(serialized);
            if ((entry.Count & 1) != 0)
            {
                entry.Add(0);
            }

            const int headerSize = 20;
            var dataArrayStart = headerSize + (glyphCount + 1) * 2;

            var table = new List<byte>();
            WriteU16(table, 1);                                     // majorVersion
            WriteU16(table, 0);                                     // minorVersion
            WriteU16(table, (ushort)axisCount);
            WriteU16(table, 0);                                     // sharedTupleCount
            WriteU32(table, (uint)dataArrayStart);                  // sharedTuplesOffset
            WriteU16(table, (ushort)glyphCount);
            WriteU16(table, 0);                                     // flags: short offsets
            WriteU32(table, (uint)dataArrayStart);                  // glyphVariationDataArrayOffset

            for (var i = 0; i <= glyphCount; i++)
            {
                WriteU16(table, (ushort)(i > glyph ? entry.Count / 2 : 0));
            }

            table.AddRange(entry);

            return table.ToArray();
        }

        private static void WritePackedWordDeltas(List<byte> data, short[] deltas)
        {
            data.Add((byte)(0x40 | (deltas.Length - 1)));           // DELTAS_ARE_WORDS, run of all values

            foreach (var delta in deltas)
            {
                WriteU16(data, (ushort)delta);
            }
        }

        private static void WriteU16(List<byte> data, ushort value)
        {
            data.Add((byte)(value >> 8));
            data.Add((byte)(value & 0xFF));
        }

        private static void WriteU32(List<byte> data, uint value)
        {
            WriteU16(data, (ushort)(value >> 16));
            WriteU16(data, (ushort)(value & 0xFFFF));
        }
    }
}
