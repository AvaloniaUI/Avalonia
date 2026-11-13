using System;
using Avalonia.Media;
using Avalonia.Media.Fonts.Tables.OpenTypeMath;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts.Tables
{
    /// <summary>
    /// Reads the MATH tables of the STIX Two Math and Latin Modern Math subsets. Expected values are
    /// taken from fontTools' parse of the same files (MathTableFixtures.py dump); glyph names are the
    /// subsets' post names.
    /// </summary>
    public class MathTableTests
    {
        private static MathTable Load(string asset)
        {
            var typeface = SyntheticFont.FromAsset(asset).CreateGlyphTypeface();

            Assert.True(MathTable.TryLoad(typeface, out var table));

            return table!;
        }

        private static int[] Constants(MathTable table)
        {
            var constants = new int[MathTable.ConstantCount];

            table.ResolveConstants(constants, null, default);

            return constants;
        }

        [Fact]
        public void Stix_Constants_Are_Read_In_Table_Order()
        {
            Assert.Equal(
                new[]
                {
                    70, 55, 1325, 1800, 150, 258, 480, 656, 210, 368, 160, 360, 252, 120, 230, 150, 380, 40,
                    135, 300, 135, 670, 470, 780, 385, 690, 150, 300, 800, 590, 68, 68, 585, 640, 585, 640,
                    68, 150, 68, 68, 150, 350, 68, 175, 68, 68, 175, 68, 68, 85, 170, 68, 78, 65, -335, 55,
                },
                Constants(Load(SyntheticFont.Assets.StixTwoMath)));
        }

        [Fact]
        public void Latin_Modern_Constants_Are_Read_In_Table_Order()
        {
            Assert.Equal(
                new[]
                {
                    70, 50, 1300, 1300, 154, 250, 450, 664, 247, 344, 200, 363, 289, 108, 250, 160, 344, 56,
                    200, 111, 167, 600, 444, 677, 345, 686, 120, 280, 111, 600, 200, 167, 394, 677, 345, 686,
                    40, 120, 40, 40, 120, 350, 96, 120, 40, 40, 120, 40, 40, 50, 148, 40, 40, 278, -556, 60,
                },
                Constants(Load(SyntheticFont.Assets.LatinModernMath)));
        }

        [Fact]
        public void Constant_Count_Matches_The_Enum()
        {
            Assert.Equal(56, MathTable.ConstantCount);
            Assert.Equal(56, Enum.GetValues<MathConstant>().Length);
        }

        [Theory]
        [InlineData(116, 230)] // uni222B, the integral
        [InlineData(117, 540)] // uni222B.dsp
        [InlineData(121, 540)] // uni222E.dsp
        public void Stix_Italics_Correction_Is_Read_For_Covered_Glyphs(int glyph, int expected)
        {
            Assert.True(Load(SyntheticFont.Assets.StixTwoMath).GlyphInfo!.TryGetItalicsCorrection((ushort)glyph, null, default, out var value));
            Assert.Equal(expected, value);
        }

        [Theory]
        [InlineData(19, 79)] // f
        [InlineData(14, 8)] // V
        [InlineData(190, 332)] // integral
        [InlineData(194, 591)] // integral.v1
        public void Latin_Modern_Italics_Correction_Is_Read_For_Covered_Glyphs(int glyph, int expected)
        {
            Assert.True(Load(SyntheticFont.Assets.LatinModernMath).GlyphInfo!.TryGetItalicsCorrection((ushort)glyph, null, default, out var value));
            Assert.Equal(expected, value);
        }

        [Fact]
        public void Italics_Correction_Misses_Uncovered_Glyphs()
        {
            // x has no italics correction in STIX Two Math.
            Assert.False(Load(SyntheticFont.Assets.StixTwoMath).GlyphInfo!.TryGetItalicsCorrection(5, null, default, out var value));
            Assert.Equal(0, value);
        }

        [Theory]
        [InlineData(SyntheticFont.Assets.StixTwoMath, 1, 360)] // A
        [InlineData(SyntheticFont.Assets.StixTwoMath, 5, 250)] // x
        [InlineData(SyntheticFont.Assets.StixTwoMath, 19, 240)] // one
        [InlineData(SyntheticFont.Assets.LatinModernMath, 13, 375)] // A
        [InlineData(SyntheticFont.Assets.LatinModernMath, 3, 254)] // zero
        public void Top_Accent_Attachment_Is_Read_For_Covered_Glyphs(string asset, int glyph, int expected)
        {
            Assert.True(Load(asset).GlyphInfo!.TryGetTopAccentAttachment((ushort)glyph, null, default, out var value));
            Assert.Equal(expected, value);
        }

        [Fact]
        public void Top_Accent_Attachment_Misses_Uncovered_Glyphs()
        {
            // The parenthesis has no accent attachment.
            Assert.False(Load(SyntheticFont.Assets.StixTwoMath).GlyphInfo!.TryGetTopAccentAttachment(9, null, default, out _));
            Assert.False(Load(SyntheticFont.Assets.LatinModernMath).GlyphInfo!.TryGetTopAccentAttachment(1, null, default, out _));
        }

        [Fact]
        public void Extended_Shapes_Are_Covered()
        {
            var stix = Load(SyntheticFont.Assets.StixTwoMath).GlyphInfo!;
            var latinModern = Load(SyntheticFont.Assets.LatinModernMath).GlyphInfo!;

            Assert.True(stix.IsExtendedShape(7)); // bar
            Assert.True(stix.IsExtendedShape(28)); // parenleft.s1
            Assert.False(stix.IsExtendedShape(5)); // x
            Assert.True(latinModern.IsExtendedShape(77)); // parenleft.v2
            Assert.False(latinModern.IsExtendedShape(1)); // parenleft
        }

        [Theory]
        // A, top right: heights 252, 352; values 0, -18, -66.
        [InlineData(1, MathKernCorner.TopRight, -1000, 0)]
        [InlineData(1, MathKernCorner.TopRight, 251, 0)]
        [InlineData(1, MathKernCorner.TopRight, 252, 0)]
        [InlineData(1, MathKernCorner.TopRight, 253, -18)]
        [InlineData(1, MathKernCorner.TopRight, 352, -18)]
        [InlineData(1, MathKernCorner.TopRight, 353, -66)]
        [InlineData(1, MathKernCorner.TopRight, 5000, -66)]
        // V, bottom right: heights 126, 280; values -193, -119, 56.
        [InlineData(2, MathKernCorner.BottomRight, 0, -193)]
        [InlineData(2, MathKernCorner.BottomRight, 200, -119)]
        [InlineData(2, MathKernCorner.BottomRight, 281, 56)]
        // f, top right and bottom right: no heights, one value each.
        [InlineData(4, MathKernCorner.TopRight, -500, 82)]
        [InlineData(4, MathKernCorner.TopRight, 500, 82)]
        [InlineData(4, MathKernCorner.BottomRight, 0, 25)]
        // seven, bottom right: heights 145, 297; values -123, -65, 45.
        [InlineData(25, MathKernCorner.BottomRight, 146, -65)]
        // Corners without a kern, and a glyph without kern info (bar).
        [InlineData(1, MathKernCorner.TopLeft, 300, 0)]
        [InlineData(1, MathKernCorner.BottomLeft, 300, 0)]
        [InlineData(2, MathKernCorner.TopRight, 300, 0)]
        [InlineData(7, MathKernCorner.TopRight, 300, 0)]
        public void Stix_Kerning_Takes_The_Value_Above_The_Heights_Strictly_Below(
            int glyph, MathKernCorner corner, int height, int expected)
        {
            var glyphInfo = Load(SyntheticFont.Assets.StixTwoMath).GlyphInfo!;

            Assert.Equal(expected, glyphInfo.GetKerning((ushort)glyph, (int)corner, height, null, default));
        }

        [Fact]
        public void Latin_Modern_Has_No_Kerning()
        {
            Assert.Equal(0, Load(SyntheticFont.Assets.LatinModernMath).GlyphInfo!.GetKerning(13, 0, 300, null, default));
        }

        [Fact]
        public void Min_Connector_Overlap_Is_Read()
        {
            Assert.Equal(100, Load(SyntheticFont.Assets.StixTwoMath).Variants!.MinConnectorOverlap);
            Assert.Equal(20, Load(SyntheticFont.Assets.LatinModernMath).Variants!.MinConnectorOverlap);
        }

        [Fact]
        public void Stix_Vertical_Variants_Are_Read_In_Increasing_Size()
        {
            // parenleft: itself and parenleft.s1 to parenleft.s12.
            var variants = Load(SyntheticFont.Assets.StixTwoMath).Variants!.GetGlyphVariants(9, MathDirection.Vertical);

            Assert.Equal(13, variants.Length);
            Assert.Equal(new MathGlyphVariant(9, 933), variants[0]);
            Assert.Equal(new MathGlyphVariant(28, 1187), variants[1]);
            Assert.Equal(new MathGlyphVariant(39, 3821), variants[12]);
        }

        [Fact]
        public void Stix_Vertical_Assembly_Lists_Parts_Bottom_To_Top()
        {
            // parenleft: uni239D (lower hook), uni239C (extension), uni239B (upper hook).
            Assert.True(Load(SyntheticFont.Assets.StixTwoMath).Variants!
                .TryGetGlyphAssembly(9, MathDirection.Vertical, null, default, out var assembly));

            Assert.Equal(0, assembly.ItalicsCorrection);
            Assert.Equal(
                new[]
                {
                    new MathGlyphPart(196, 0, 250, 1273, false),
                    new MathGlyphPart(195, 1000, 1000, 1252, true),
                    new MathGlyphPart(194, 250, 0, 1273, false),
                },
                assembly.Parts.ToArray());
        }

        [Fact]
        public void Stix_Integral_Assembly_Has_An_Italics_Correction()
        {
            Assert.True(Load(SyntheticFont.Assets.StixTwoMath).Variants!
                .TryGetGlyphAssembly(116, MathDirection.Vertical, null, default, out var assembly));

            Assert.Equal(80, assembly.ItalicsCorrection);
            Assert.Equal(
                new[]
                {
                    new MathGlyphPart(193, 0, 800, 1896, false),
                    new MathGlyphPart(191, 600, 600, 1251, true),
                    new MathGlyphPart(192, 800, 0, 1630, false),
                },
                assembly.Parts.ToArray());
        }

        [Fact]
        public void Stix_Horizontal_Arrow_Has_Variants_And_An_Assembly_Left_To_Right()
        {
            var variants = Load(SyntheticFont.Assets.StixTwoMath).Variants!;

            Assert.Equal(new[] { new MathGlyphVariant(100, 948) }, variants.GetGlyphVariants(100, MathDirection.Horizontal).ToArray());
            Assert.True(variants.TryGetGlyphAssembly(100, MathDirection.Horizontal, null, default, out var assembly));
            Assert.Equal(
                new[]
                {
                    new MathGlyphPart(190, 250, 250, 597, true),
                    new MathGlyphPart(101, 0, 200, 505, false),
                },
                assembly.Parts.ToArray());
        }

        [Fact]
        public void Latin_Modern_Radical_Has_Variants_And_An_Assembly()
        {
            var variants = Load(SyntheticFont.Assets.LatinModernMath).Variants!;

            Assert.Equal(
                new[]
                {
                    new MathGlyphVariant(198, 1001), new MathGlyphVariant(202, 1201), new MathGlyphVariant(203, 1801),
                    new MathGlyphVariant(204, 2401), new MathGlyphVariant(205, 3001),
                },
                variants.GetGlyphVariants(198, MathDirection.Vertical).ToArray());
            Assert.True(variants.TryGetGlyphAssembly(198, MathDirection.Vertical, null, default, out var assembly));
            Assert.Equal(
                new[]
                {
                    new MathGlyphPart(199, 0, 320, 1820, false),
                    new MathGlyphPart(200, 640, 640, 640, true),
                    new MathGlyphPart(201, 320, 0, 620, false),
                },
                assembly.Parts.ToArray());
        }

        [Fact]
        public void Latin_Modern_Horizontal_Brace_Has_Five_Parts()
        {
            Assert.True(Load(SyntheticFont.Assets.LatinModernMath).Variants!
                .TryGetGlyphAssembly(67, MathDirection.Horizontal, null, default, out var assembly));

            Assert.Equal(
                new[]
                {
                    new MathGlyphPart(145, 0, 497, 1002, false),
                    new MathGlyphPart(146, 994, 994, 994, true),
                    new MathGlyphPart(147, 497, 497, 2003, false),
                    new MathGlyphPart(146, 994, 994, 994, true),
                    new MathGlyphPart(148, 497, 0, 1001, false),
                },
                assembly.Parts.ToArray());
        }

        [Fact]
        public void Construction_Without_An_Assembly_Has_Variants_Only()
        {
            // uni2211, the summation: two sizes, no assembly.
            var variants = Load(SyntheticFont.Assets.StixTwoMath).Variants!;

            Assert.Equal(
                new[] { new MathGlyphVariant(105, 1031), new MathGlyphVariant(106, 1326) },
                variants.GetGlyphVariants(105, MathDirection.Vertical).ToArray());
            Assert.False(variants.TryGetGlyphAssembly(105, MathDirection.Vertical, null, default, out _));
        }

        [Fact]
        public void Glyphs_Without_A_Construction_In_A_Direction_Have_None()
        {
            var variants = Load(SyntheticFont.Assets.StixTwoMath).Variants!;

            Assert.True(variants.GetGlyphVariants(5, MathDirection.Vertical).IsEmpty); // x
            Assert.True(variants.GetGlyphVariants(9, MathDirection.Horizontal).IsEmpty); // parenleft
            Assert.True(variants.GetGlyphVariants(100, MathDirection.Vertical).IsEmpty); // uni2192
            Assert.False(variants.TryGetGlyphAssembly(9, MathDirection.Horizontal, null, default, out _));
        }

        [Fact]
        public void Repeated_Variant_Requests_Return_The_Cached_Array()
        {
            var variants = Load(SyntheticFont.Assets.StixTwoMath).Variants!;

            var first = variants.GetGlyphVariants(9, MathDirection.Vertical);
            var second = variants.GetGlyphVariants(9, MathDirection.Vertical);

            Assert.True(first == second);
        }

        [Fact]
        public void Font_Without_MATH_Has_No_Table()
        {
            var typeface = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular).CreateGlyphTypeface();

            Assert.False(MathTable.TryLoad(typeface, out _));
        }
    }
}
