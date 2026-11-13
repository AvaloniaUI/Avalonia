using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class GlyphTypefaceMathTableTests
    {
        [Fact]
        public void Font_Without_MATH_Has_No_Math_Table()
        {
            Assert.Null(SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular).CreateGlyphTypeface().MathTable);
            Assert.Null(SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable).CreateGlyphTypeface().MathTable);
        }

        [Fact]
        public void Math_Fonts_Expose_Their_Table()
        {
            var stix = SyntheticFont.FromAsset(SyntheticFont.Assets.StixTwoMath).CreateGlyphTypeface().MathTable;
            var latinModern = SyntheticFont.FromAsset(SyntheticFont.Assets.LatinModernMath).CreateGlyphTypeface().MathTable;

            Assert.NotNull(stix);
            Assert.NotNull(latinModern);
            Assert.Equal(258, stix!.GetConstant(MathConstant.AxisHeight));
            Assert.Equal(250, latinModern!.GetConstant(MathConstant.AxisHeight));
            Assert.Equal(-335, stix.GetConstant(MathConstant.RadicalKernAfterDegree));
            Assert.Equal(55, stix.GetConstant(MathConstant.RadicalDegreeBottomRaisePercent));
        }

        [Fact]
        public void Repeated_Access_Returns_The_Same_Table()
        {
            var typeface = SyntheticFont.FromAsset(SyntheticFont.Assets.StixTwoMath).CreateGlyphTypeface();

            Assert.Same(typeface.MathTable, typeface.MathTable);
        }

        [Fact]
        public void Concurrent_First_Access_Returns_Equal_Tables()
        {
            var typeface = SyntheticFont.FromAsset(SyntheticFont.Assets.StixTwoMath).CreateGlyphTypeface();
            const int threads = 8;
            using var barrier = new Barrier(threads);
            var results = new OpenTypeMathTable?[threads];

            Parallel.For(0, threads, new ParallelOptions { MaxDegreeOfParallelism = threads }, i =>
            {
                barrier.SignalAndWait();
                results[i] = typeface.MathTable;
            });

            Assert.All(results, table =>
            {
                Assert.NotNull(table);
                Assert.Equal(258, table!.GetConstant(MathConstant.AxisHeight));
                Assert.Equal(13, table.GetGlyphVariants(9, MathDirection.Vertical).Length);
            });
            Assert.Contains(typeface.MathTable, results);
        }

        [Fact]
        public void Simulated_Typeface_Returns_The_Designed_Values()
        {
            var font = SyntheticFont.FromAsset(SyntheticFont.Assets.StixTwoMath);
            var regular = font.CreateGlyphTypeface().MathTable!;
            var simulated = font.CreateGlyphTypeface(FontSimulations.Bold | FontSimulations.Oblique).MathTable!;

            foreach (var constant in Enum.GetValues<MathConstant>())
            {
                Assert.Equal(regular.GetConstant(constant), simulated.GetConstant(constant));
            }

            Assert.Equal(regular.GetItalicsCorrection(116), simulated.GetItalicsCorrection(116));
            Assert.Equal(
                regular.GetKerning(1, MathKernCorner.TopRight, 300),
                simulated.GetKerning(1, MathKernCorner.TopRight, 300));
        }

        [Fact]
        public void Glyphs_Without_Data_Return_Defaults()
        {
            var table = SyntheticFont.FromAsset(SyntheticFont.Assets.StixTwoMath).CreateGlyphTypeface().MathTable!;

            Assert.Equal(0, table.GetItalicsCorrection(5));
            Assert.False(table.TryGetTopAccentAttachment(9, out var x));
            Assert.Equal(0, x);
            Assert.False(table.IsExtendedShape(5));
            Assert.Equal(0, table.GetKerning(7, MathKernCorner.TopRight, 0));
            Assert.True(table.GetGlyphVariants(5, MathDirection.Vertical).IsEmpty);
            Assert.False(table.TryGetGlyphAssembly(5, MathDirection.Vertical, out var assembly));
            Assert.Equal(default, assembly);
        }

        [Fact]
        public void Public_Lookups_Match_The_Font()
        {
            var table = SyntheticFont.FromAsset(SyntheticFont.Assets.StixTwoMath).CreateGlyphTypeface().MathTable!;

            Assert.Equal(100, table.MinConnectorOverlap);
            Assert.Equal(230, table.GetItalicsCorrection(116));
            Assert.True(table.TryGetTopAccentAttachment(1, out var x));
            Assert.Equal(360, x);
            Assert.True(table.IsExtendedShape(7));
            Assert.Equal(-18, table.GetKerning(1, MathKernCorner.TopRight, 300));
            Assert.Equal(
                new[] { new MathGlyphVariant(105, 1031), new MathGlyphVariant(106, 1326) },
                table.GetGlyphVariants(105, MathDirection.Vertical).ToArray());
            Assert.True(table.TryGetGlyphAssembly(116, MathDirection.Vertical, out var assembly));
            Assert.Equal(80, assembly.ItalicsCorrection);
            Assert.Equal(new ushort[] { 193, 191, 192 }, assembly.Parts.ToArray().Select(p => p.Glyph).ToArray());
        }

        [Fact]
        public void Undefined_Enum_Values_Throw()
        {
            var table = SyntheticFont.FromAsset(SyntheticFont.Assets.StixTwoMath).CreateGlyphTypeface().MathTable!;

            Assert.Throws<ArgumentOutOfRangeException>(() => table.GetConstant((MathConstant)56));
            Assert.Throws<ArgumentOutOfRangeException>(() => table.GetConstant((MathConstant)(-1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => table.GetKerning(1, (MathKernCorner)4, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => table.GetGlyphVariants(9, (MathDirection)2));
            Assert.Throws<ArgumentOutOfRangeException>(() => table.TryGetGlyphAssembly(9, (MathDirection)2, out _));
        }
    }
}
