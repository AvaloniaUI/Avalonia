using System;
using System.Buffers.Binary;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;
using Part = Avalonia.Base.UnitTests.Media.Fonts.Tables.MathTableBuilder.Part;
using Value = Avalonia.Base.UnitTests.Media.Fonts.Tables.MathTableBuilder.Value;

namespace Avalonia.Base.UnitTests.Media.Fonts.Tables
{
    /// <summary>
    /// A synthetic MATH table on InterVariable whose device records point into Inter's real GDEF
    /// variation store. Expected deltas come from fontTools' VarStoreInstancer
    /// (MathTableFixtures.py deltas), rounded to the nearest integer:
    /// <code>
    /// record   wght=700  wght=650  Thin (wght=100)  opsz=32 wght=900
    /// 0:0      -64.26    -53.55     0               -119
    /// 0:1      -63.18    -52.65     0               -117
    /// 3:0       37.80     31.50   -96                 70
    /// 3:1       29.16     24.30   -68                 54
    /// 5:0        0         0        0                -69
    /// 5:2        0         0        0                -62
    /// </code>
    /// </summary>
    public class MathTableVariationTests
    {
        private const ushort ItalicGlyph = 10;
        private const ushort KernGlyph = 11;
        private const ushort StretchyGlyph = 12;

        private static byte[] MathTable() => new MathTableBuilder()
            .Constant((int)MathConstant.ScriptPercentScaleDown, 70)
            .Constant((int)MathConstant.DelimitedSubFormulaMinHeight, 1300)
            .Constant((int)MathConstant.MathLeading, 150, (3, 0))
            .Constant((int)MathConstant.AxisHeight, 250, (0, 0))
            .Constant((int)MathConstant.FractionRuleThickness, 40, (5, 0))
            .ItalicsCorrection(ItalicGlyph, 30, (3, 1))
            .TopAccentAttachment(ItalicGlyph, 200, (0, 1))
            .Kern(KernGlyph, (int)MathKernCorner.TopRight,
                new[] { new Value(300, (0, 0)) },
                new[] { new Value(-10), new Value(20, (3, 0)) })
            .Vertical(StretchyGlyph, new[] { (StretchyGlyph, 1000), (13, 1500) },
                new MathTableBuilder.Assembly(new Value(25, (5, 2)),
                    new[] { new Part(14, 0, 100, 500, 0), new Part(15, 100, 100, 300, 1) }))
            .Build();

        private static SyntheticFont InterWithMath() =>
            SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable).Replace("MATH", MathTable());

        private static GlyphTypeface CreateTypeface() => InterWithMath().CreateGlyphTypeface();

        private static (int AxisHeight, int MathLeading, int RuleThickness, int Italics, int Accent, int AssemblyItalics) Values(
            OpenTypeMathTable table)
        {
            Assert.True(table.TryGetTopAccentAttachment(ItalicGlyph, out var accent));
            Assert.True(table.TryGetGlyphAssembly(StretchyGlyph, MathDirection.Vertical, out var assembly));

            return (
                table.GetConstant(MathConstant.AxisHeight),
                table.GetConstant(MathConstant.MathLeading),
                table.GetConstant(MathConstant.FractionRuleThickness),
                table.GetItalicsCorrection(ItalicGlyph),
                accent,
                assembly.ItalicsCorrection);
        }

        [Fact]
        public void Default_Instance_Returns_The_Designed_Values()
        {
            Assert.Equal((250, 150, 40, 30, 200, 25), Values(CreateTypeface().MathTable!));
        }

        [Fact]
        public void Weight_700_Applies_The_Deltas()
        {
            var clone = CreateTypeface().WithVariations(FontVariationSettings.Parse("wght=700"));

            Assert.Equal((250 - 64, 150 + 38, 40, 30 + 29, 200 - 63, 25), Values(clone.MathTable!));
        }

        [Fact]
        public void Weight_650_Is_Mapped_Through_Avar()
        {
            var clone = CreateTypeface().WithVariations(FontVariationSettings.Parse("wght=650"));

            Assert.Equal((250 - 54, 150 + 31, 40, 30 + 24, 200 - 53, 25), Values(clone.MathTable!));
        }

        [Fact]
        public void Named_Instance_Applies_Its_Deltas()
        {
            // Instance 0 is Thin, wght=100.
            var thin = CreateTypeface().WithVariations(null, 0);

            Assert.Equal((250, 150 - 96, 40, 30 - 68, 200, 25), Values(thin.MathTable!));
        }

        [Fact]
        public void Deltas_Of_Several_Axes_Are_Summed()
        {
            var clone = CreateTypeface().WithVariations(FontVariationSettings.Parse("opsz=32, wght=900"));

            Assert.Equal((250 - 119, 150 + 70, 40 - 69, 30 + 54, 200 - 117, 25 - 62), Values(clone.MathTable!));
        }

        [Fact]
        public void Kern_Heights_And_Values_Are_Varied()
        {
            var typeface = CreateTypeface();
            var bold = typeface.WithVariations(FontVariationSettings.Parse("wght=700"));

            // The correction height is 300 at the default instance and 300 - 64 = 236 at wght=700.
            Assert.Equal(-10, typeface.MathTable!.GetKerning(KernGlyph, MathKernCorner.TopRight, 250));
            Assert.Equal(20 + 38, bold.MathTable!.GetKerning(KernGlyph, MathKernCorner.TopRight, 250));
            Assert.Equal(-10, bold.MathTable.GetKerning(KernGlyph, MathKernCorner.TopRight, 236));
        }

        [Fact]
        public void Instances_Share_The_Parsed_Table_But_Resolve_Independently()
        {
            var typeface = CreateTypeface();
            var bold = typeface.WithVariations(FontVariationSettings.Parse("wght=700"));
            var thin = typeface.WithVariations(FontVariationSettings.Parse("wght=100"));

            Assert.Same(typeface.MathTable!.Data, bold.MathTable!.Data);
            Assert.Same(typeface.MathTable.Data, thin.MathTable!.Data);
            Assert.NotSame(bold.MathTable, thin.MathTable);

            Assert.Equal(150 + 38, bold.MathTable.GetConstant(MathConstant.MathLeading));
            Assert.Equal(150 - 96, thin.MathTable.GetConstant(MathConstant.MathLeading));
            Assert.Equal(150, typeface.MathTable.GetConstant(MathConstant.MathLeading));
        }

        [Fact]
        public void Variants_And_Parts_Are_The_Same_At_Every_Instance()
        {
            var typeface = CreateTypeface();
            var bold = typeface.WithVariations(FontVariationSettings.Parse("wght=700"));

            var defaultVariants = typeface.MathTable!.GetGlyphVariants(StretchyGlyph, MathDirection.Vertical);
            var boldVariants = bold.MathTable!.GetGlyphVariants(StretchyGlyph, MathDirection.Vertical);

            Assert.True(defaultVariants == boldVariants);
            Assert.Equal(new[] { new MathGlyphVariant(StretchyGlyph, 1000), new MathGlyphVariant(13, 1500) }, boldVariants.ToArray());

            Assert.True(typeface.MathTable.TryGetGlyphAssembly(StretchyGlyph, MathDirection.Vertical, out var defaultAssembly));
            Assert.True(bold.MathTable.TryGetGlyphAssembly(StretchyGlyph, MathDirection.Vertical, out var boldAssembly));
            Assert.True(defaultAssembly.Parts.Span == boldAssembly.Parts.Span);
        }

        [Fact]
        public void Unvaried_Constants_Stay_At_Their_Designed_Values()
        {
            var clone = CreateTypeface().WithVariations(FontVariationSettings.Parse("opsz=32, wght=900"));

            Assert.Equal(70, clone.MathTable!.GetConstant(MathConstant.ScriptPercentScaleDown));
            Assert.Equal(1300, clone.MathTable.GetConstant(MathConstant.DelimitedSubFormulaMinHeight));
        }

        [Fact]
        public void Store_With_The_Wrong_Axis_Count_Leaves_The_Values_Unvaried()
        {
            var font = InterWithMath();
            var gdef = font.GetTable("GDEF");
            var store = (int)BinaryPrimitives.ReadUInt32BigEndian(gdef.AsSpan(14));
            var regionList = (int)BinaryPrimitives.ReadUInt32BigEndian(gdef.AsSpan(store + 2));
            font.PatchUInt16("GDEF", store + regionList, 3);

            var clone = font.CreateGlyphTypeface().WithVariations(FontVariationSettings.Parse("wght=700"));

            Assert.Equal((250, 150, 40, 30, 200, 25), Values(clone.MathTable!));
        }

        [Fact]
        public void Font_Without_GDEF_Leaves_The_Values_Unvaried()
        {
            var clone = InterWithMath().Remove("GDEF").CreateGlyphTypeface()
                .WithVariations(FontVariationSettings.Parse("wght=700"));

            Assert.Equal((250, 150, 40, 30, 200, 25), Values(clone.MathTable!));
        }

        [Fact]
        public void Static_Font_Clone_Returns_The_Same_Table()
        {
            var typeface = SyntheticFont.FromAsset(SyntheticFont.Assets.StixTwoMath).CreateGlyphTypeface();
            var clone = typeface.WithVariations(FontVariationSettings.Parse("wght=700"));

            Assert.Same(typeface.MathTable, clone.MathTable);
        }
    }
}
