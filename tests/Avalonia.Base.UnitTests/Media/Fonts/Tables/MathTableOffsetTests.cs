using Avalonia.Media;
using Avalonia.Media.Fonts.Tables;
using Avalonia.Media.Fonts.Tables.OpenTypeMath;
using Avalonia.Media.Fonts.Tables.Variation;
using Avalonia.UnitTests;
using Xunit;
using Part = Avalonia.Base.UnitTests.Media.Fonts.Tables.MathTableBuilder.Part;
using Value = Avalonia.Base.UnitTests.Media.Fonts.Tables.MathTableBuilder.Value;

namespace Avalonia.Base.UnitTests.Media.Fonts.Tables
{
    /// <summary>
    /// Every MATH offset is relative to a specific parent table. The builder writes filler before
    /// each sub-table, so a reader that resolves an offset against the wrong parent reads filler and
    /// these tests fail.
    /// </summary>
    /// <remarks>
    /// Device records point into InterVariable's GDEF store at normalized wght 1.0, where fontTools'
    /// VarStoreInstancer gives delta set (0, 0) a delta of -119 and (3, 0) a delta of +70.
    /// </remarks>
    public class MathTableOffsetTests
    {
        private const int DeltaOuter0Inner0 = -119;
        private const int DeltaOuter3Inner0 = 70;

        private static readonly (int, int) s_minus119 = (0, 0);
        private static readonly (int, int) s_plus70 = (3, 0);

        private readonly ItemVariationStore _store;
        private readonly float[] _scalers;

        public MathTableOffsetTests()
        {
            var inter = SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable).CreateGlyphTypeface();

            Assert.True(GdefTable.TryLoad(inter, 2, out var gdef));

            _store = gdef!.VariationStore!;
            _scalers = new float[_store.RegionCount];
            _store.ComputeRegionScalers(new[] { 0f, 1f }, _scalers);
        }

        private static MathTable Load(MathTableBuilder builder)
        {
            Assert.True(MathTable.TryLoad(builder.Build(), out var table));

            return table!;
        }

        public static TheoryData<int, int> Layouts => new()
        {
            { 1, 1 },
            { 6, 1 },
            { 7, 2 },
        };

        [Theory]
        [MemberData(nameof(Layouts))]
        public void Constants_Device_Offsets_Are_Relative_To_MathConstants(int padding, int coverageFormat)
        {
            var table = Load(new MathTableBuilder { Padding = padding, CoverageFormat = coverageFormat }
                .Constant((int)MathConstant.AxisHeight, 250, s_minus119)
                .Constant((int)MathConstant.RadicalKernAfterDegree, -300, s_plus70)
                .Constant((int)MathConstant.RadicalDegreeBottomRaisePercent, 60));

            var constants = new int[MathTable.ConstantCount];
            table.ResolveConstants(constants, _store, _scalers);

            Assert.Equal(250 + DeltaOuter0Inner0, constants[(int)MathConstant.AxisHeight]);
            Assert.Equal(-300 + DeltaOuter3Inner0, constants[(int)MathConstant.RadicalKernAfterDegree]);
            Assert.Equal(60, constants[(int)MathConstant.RadicalDegreeBottomRaisePercent]);

            table.ResolveConstants(constants, _store, default);

            Assert.Equal(250, constants[(int)MathConstant.AxisHeight]);
        }

        [Theory]
        [MemberData(nameof(Layouts))]
        public void Italics_Corrections_Are_Relative_To_MathItalicsCorrectionInfo(int padding, int coverageFormat)
        {
            var glyphInfo = Load(new MathTableBuilder { Padding = padding, CoverageFormat = coverageFormat }
                .ItalicsCorrection(10, 40)
                .ItalicsCorrection(11, 50, s_plus70)
                .ItalicsCorrection(30, 60)).GlyphInfo!;

            Assert.True(glyphInfo.TryGetItalicsCorrection(10, _store, _scalers, out var unvaried));
            Assert.True(glyphInfo.TryGetItalicsCorrection(11, _store, _scalers, out var varied));
            Assert.True(glyphInfo.TryGetItalicsCorrection(30, _store, _scalers, out var last));

            Assert.Equal(40, unvaried);
            Assert.Equal(50 + DeltaOuter3Inner0, varied);
            Assert.Equal(60, last);
        }

        [Theory]
        [MemberData(nameof(Layouts))]
        public void Top_Accent_Attachments_Are_Relative_To_MathTopAccentAttachment(int padding, int coverageFormat)
        {
            var glyphInfo = Load(new MathTableBuilder { Padding = padding, CoverageFormat = coverageFormat }
                .ItalicsCorrection(10, 1)
                .TopAccentAttachment(10, 300, s_minus119)
                .TopAccentAttachment(12, 320)).GlyphInfo!;

            Assert.True(glyphInfo.TryGetTopAccentAttachment(10, _store, _scalers, out var varied));
            Assert.True(glyphInfo.TryGetTopAccentAttachment(12, _store, _scalers, out var unvaried));

            Assert.Equal(300 + DeltaOuter0Inner0, varied);
            Assert.Equal(320, unvaried);
        }

        [Theory]
        [MemberData(nameof(Layouts))]
        public void Extended_Shape_Coverage_Is_Relative_To_MathGlyphInfo(int padding, int coverageFormat)
        {
            var glyphInfo = Load(new MathTableBuilder { Padding = padding, CoverageFormat = coverageFormat }
                .ItalicsCorrection(3, 1)
                .ExtendedShape(20)
                .ExtendedShape(21)).GlyphInfo!;

            Assert.True(glyphInfo.IsExtendedShape(20));
            Assert.True(glyphInfo.IsExtendedShape(21));
            Assert.False(glyphInfo.IsExtendedShape(3));
        }

        [Theory]
        [MemberData(nameof(Layouts))]
        public void Kern_Offsets_Are_Relative_To_MathKernInfo_And_Records_To_MathKern(int padding, int coverageFormat)
        {
            var glyphInfo = Load(new MathTableBuilder { Padding = padding, CoverageFormat = coverageFormat }
                .Kern(5, (int)MathKernCorner.TopRight, new Value[] { 100, 200 }, new Value[] { -10, -20, -30 })
                .Kern(5, (int)MathKernCorner.BottomLeft, new Value[] { 50 }, new Value[] { 7, 8 })
                .Kern(6, (int)MathKernCorner.BottomRight,
                    new[] { new Value(300, s_minus119) },
                    new[] { new Value(-5), new Value(10, s_plus70) })).GlyphInfo!;

            Assert.Equal(-10, glyphInfo.GetKerning(5, (int)MathKernCorner.TopRight, 100, _store, _scalers));
            Assert.Equal(-20, glyphInfo.GetKerning(5, (int)MathKernCorner.TopRight, 150, _store, _scalers));
            Assert.Equal(-30, glyphInfo.GetKerning(5, (int)MathKernCorner.TopRight, 201, _store, _scalers));
            Assert.Equal(8, glyphInfo.GetKerning(5, (int)MathKernCorner.BottomLeft, 51, _store, _scalers));
            Assert.Equal(0, glyphInfo.GetKerning(5, (int)MathKernCorner.TopLeft, 51, _store, _scalers));

            // The height varies to 300 - 119 = 181, so 200 lies above it and takes the varied value.
            Assert.Equal(10 + DeltaOuter3Inner0, glyphInfo.GetKerning(6, (int)MathKernCorner.BottomRight, 200, _store, _scalers));
            Assert.Equal(-5, glyphInfo.GetKerning(6, (int)MathKernCorner.BottomRight, 200, _store, default));
        }

        [Theory]
        [MemberData(nameof(Layouts))]
        public void Construction_Offsets_Are_Relative_To_MathVariants(int padding, int coverageFormat)
        {
            var variants = Load(new MathTableBuilder { Padding = padding, CoverageFormat = coverageFormat, MinConnectorOverlap = 33 }
                .Vertical(40, new[] { (40, 1000), (41, 1500) })
                .Vertical(42, new[] { (42, 900) })
                .Horizontal(50, new[] { (50, 600), (51, 1200), (52, 1800) })).Variants!;

            Assert.Equal(33, variants.MinConnectorOverlap);
            Assert.Equal(
                new[] { new MathGlyphVariant(40, 1000), new MathGlyphVariant(41, 1500) },
                variants.GetGlyphVariants(40, MathDirection.Vertical).ToArray());
            Assert.Equal(new[] { new MathGlyphVariant(42, 900) }, variants.GetGlyphVariants(42, MathDirection.Vertical).ToArray());
            Assert.Equal(
                new[] { new MathGlyphVariant(50, 600), new MathGlyphVariant(51, 1200), new MathGlyphVariant(52, 1800) },
                variants.GetGlyphVariants(50, MathDirection.Horizontal).ToArray());
        }

        [Theory]
        [MemberData(nameof(Layouts))]
        public void Assembly_Is_Relative_To_Its_Construction_And_Its_Italics_To_The_Assembly(int padding, int coverageFormat)
        {
            var parts = new[]
            {
                new Part(60, 0, 100, 500, 0),
                new Part(61, 200, 200, 400, 1),
                new Part(62, 100, 0, 500, 0),
            };

            var variants = Load(new MathTableBuilder { Padding = padding, CoverageFormat = coverageFormat }
                .Vertical(44, new[] { (44, 1000) })
                .Vertical(45, new[] { (45, 1000) }, new MathTableBuilder.Assembly(new Value(90, s_plus70), parts))
                .Horizontal(46, new[] { (46, 800) }, new MathTableBuilder.Assembly(15, new[] { new Part(63, 5, 6, 7, 1) })))
                .Variants!;

            Assert.False(variants.TryGetGlyphAssembly(44, MathDirection.Vertical, _store, _scalers, out _));

            Assert.True(variants.TryGetGlyphAssembly(45, MathDirection.Vertical, _store, _scalers, out var vertical));
            Assert.Equal(90 + DeltaOuter3Inner0, vertical.ItalicsCorrection);
            Assert.Equal(
                new[]
                {
                    new MathGlyphPart(60, 0, 100, 500, false),
                    new MathGlyphPart(61, 200, 200, 400, true),
                    new MathGlyphPart(62, 100, 0, 500, false),
                },
                vertical.Parts.ToArray());

            Assert.True(variants.TryGetGlyphAssembly(46, MathDirection.Horizontal, _store, _scalers, out var horizontal));
            Assert.Equal(15, horizontal.ItalicsCorrection);
            Assert.Equal(new[] { new MathGlyphPart(63, 5, 6, 7, true) }, horizontal.Parts.ToArray());
        }
    }
}
