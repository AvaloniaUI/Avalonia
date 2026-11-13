using System;
using System.Buffers.Binary;
using Avalonia.Media;
using Avalonia.Media.Fonts.Tables;
using Avalonia.Media.Fonts.Tables.OpenTypeMath;
using Avalonia.UnitTests;
using Xunit;
using Part = Avalonia.Base.UnitTests.Media.Fonts.Tables.MathTableBuilder.Part;
using Value = Avalonia.Base.UnitTests.Media.Fonts.Tables.MathTableBuilder.Value;

namespace Avalonia.Base.UnitTests.Media.Fonts.Tables
{
    /// <summary>
    /// Out-of-range offsets and counts make only the affected part of the MATH table empty: no
    /// lookup throws, and the other parts keep their values.
    /// </summary>
    public class MathTableMalformedTests
    {
        private static MathTableBuilder Full() => new MathTableBuilder { MinConnectorOverlap = 25 }
            .Constant((int)MathConstant.AxisHeight, 250, (0, 0))
            .ItalicsCorrection(10, 40)
            .ItalicsCorrection(11, 50, (3, 0))
            .TopAccentAttachment(10, 300)
            .ExtendedShape(20)
            .Kern(5, (int)MathKernCorner.TopRight, new Value[] { 100 }, new Value[] { -10, -20 })
            .Kern(6, (int)MathKernCorner.TopRight, new Value[] { 100 }, new Value[] { -30, -40 })
            .Vertical(40, new[] { (40, 1000), (41, 1500) },
                new MathTableBuilder.Assembly(5, new[] { new Part(60, 0, 100, 500, 0), new Part(61, 200, 200, 400, 1) }))
            .Vertical(42, new[] { (42, 900) });

        private static int U16(byte[] data, int position) => BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(position));

        private static void Patch(byte[] data, int position, int value)
            => BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(position), (ushort)value);

        private static int GlyphInfoStart(byte[] data) => U16(data, 6);

        private static int VariantsStart(byte[] data) => U16(data, 8);

        private static int ItalicsStart(byte[] data) => GlyphInfoStart(data) + U16(data, GlyphInfoStart(data));

        private static int KernInfoStart(byte[] data) => GlyphInfoStart(data) + U16(data, GlyphInfoStart(data) + 6);

        // The construction offsets follow the 10-byte MathVariants header, vertical first.
        private static int ConstructionStart(byte[] data, int index)
            => VariantsStart(data) + U16(data, VariantsStart(data) + 10 + index * 2);

        private static MathTable Load(byte[] data)
        {
            Assert.True(MathTable.TryLoad(data, out var table));

            return table!;
        }

        [Fact]
        public void Truncated_Header_Is_Rejected()
        {
            Assert.False(MathTable.TryLoad(Full().Build().AsMemory(0, 9), out _));
        }

        [Fact]
        public void Unsupported_Major_Version_Is_Rejected()
        {
            var data = Full().Build();
            Patch(data, 0, 2);

            Assert.False(MathTable.TryLoad(data, out _));
        }

        [Fact]
        public void Table_Without_Usable_Sub_Tables_Is_Rejected()
        {
            var builder = new MathTableBuilder { IncludeConstants = false, IncludeGlyphInfo = false, IncludeVariants = false };

            Assert.False(MathTable.TryLoad(builder.Build(), out _));

            var data = Full().Build();
            Patch(data, 4, data.Length);
            Patch(data, 6, data.Length);
            Patch(data, 8, data.Length);

            Assert.False(MathTable.TryLoad(data, out _));
        }

        [Fact]
        public void Constants_Too_Short_For_The_Record_Leave_All_Constants_Zero()
        {
            var data = Full().Build();
            Patch(data, 4, data.Length - 100);

            var table = Load(data);
            var constants = new int[MathTable.ConstantCount];
            constants.AsSpan().Fill(-1);
            table.ResolveConstants(constants, null, default);

            Assert.False(table.HasConstants);
            Assert.All(constants, c => Assert.Equal(0, c));
            Assert.True(table.GlyphInfo!.TryGetItalicsCorrection(10, null, default, out var italics));
            Assert.Equal(40, italics);
        }

        [Fact]
        public void Glyph_Info_Offset_Past_The_End_Leaves_The_Other_Sub_Tables()
        {
            var data = Full().Build();
            Patch(data, 6, data.Length);

            var table = Load(data);

            Assert.Null(table.GlyphInfo);
            Assert.True(table.HasConstants);
            Assert.Equal(25, table.Variants!.MinConnectorOverlap);
        }

        [Fact]
        public void Variants_Offset_Past_The_End_Leaves_The_Other_Sub_Tables()
        {
            var data = Full().Build();
            Patch(data, 8, data.Length);

            var table = Load(data);

            Assert.Null(table.Variants);
            Assert.NotNull(table.GlyphInfo);
        }

        [Fact]
        public void Italics_Count_Larger_Than_The_Data_Drops_Only_The_Italics()
        {
            var data = Full().Build();
            Patch(data, ItalicsStart(data) + 2, 0xFFFF);

            var glyphInfo = Load(data).GlyphInfo!;

            Assert.False(glyphInfo.TryGetItalicsCorrection(10, null, default, out _));
            Assert.True(glyphInfo.TryGetTopAccentAttachment(10, null, default, out var accent));
            Assert.Equal(300, accent);
            Assert.True(glyphInfo.IsExtendedShape(20));
        }

        [Fact]
        public void Malformed_Italics_Coverage_Drops_Only_The_Italics()
        {
            var data = Full().Build();
            var coverage = ItalicsStart(data) + U16(data, ItalicsStart(data));
            Patch(data, coverage, 3);

            var glyphInfo = Load(data).GlyphInfo!;

            Assert.False(glyphInfo.TryGetItalicsCorrection(10, null, default, out _));
            Assert.True(glyphInfo.TryGetTopAccentAttachment(10, null, default, out _));
        }

        [Fact]
        public void Coverage_Index_Past_The_Record_Count_Is_A_Miss()
        {
            var data = Full().Build();
            Patch(data, ItalicsStart(data) + 2, 1);

            var glyphInfo = Load(data).GlyphInfo!;

            Assert.True(glyphInfo.TryGetItalicsCorrection(10, null, default, out _));
            Assert.False(glyphInfo.TryGetItalicsCorrection(11, null, default, out _));
        }

        [Fact]
        public void Kern_Height_Count_Larger_Than_The_Data_Drops_Only_That_Kern()
        {
            var data = Full().Build();
            var kernInfo = KernInfoStart(data);
            var firstKern = kernInfo + U16(data, kernInfo + 4);
            Patch(data, firstKern, 0xFFFF);

            var glyphInfo = Load(data).GlyphInfo!;

            Assert.Equal(0, glyphInfo.GetKerning(5, (int)MathKernCorner.TopRight, 200, null, default));
            Assert.Equal(-40, glyphInfo.GetKerning(6, (int)MathKernCorner.TopRight, 200, null, default));
        }

        [Fact]
        public void Kern_Offset_Past_The_End_Drops_Only_That_Kern()
        {
            var data = Full().Build();
            Patch(data, KernInfoStart(data) + 4, 0xFFF0);

            var glyphInfo = Load(data).GlyphInfo!;

            Assert.Equal(0, glyphInfo.GetKerning(5, (int)MathKernCorner.TopRight, 200, null, default));
            Assert.Equal(-40, glyphInfo.GetKerning(6, (int)MathKernCorner.TopRight, 200, null, default));
        }

        [Fact]
        public void Kern_Info_Count_Larger_Than_The_Data_Drops_All_Kerning()
        {
            var data = Full().Build();
            Patch(data, KernInfoStart(data) + 2, 0xFFFF);

            var glyphInfo = Load(data).GlyphInfo!;

            Assert.Equal(0, glyphInfo.GetKerning(6, (int)MathKernCorner.TopRight, 200, null, default));
            Assert.True(glyphInfo.TryGetItalicsCorrection(10, null, default, out _));
        }

        [Fact]
        public void Construction_Offset_Past_The_End_Drops_Only_That_Construction()
        {
            var data = Full().Build();
            Patch(data, VariantsStart(data) + 10, 0xFFF0);

            var variants = Load(data).Variants!;

            Assert.True(variants.GetGlyphVariants(40, MathDirection.Vertical).IsEmpty);
            Assert.False(variants.TryGetGlyphAssembly(40, MathDirection.Vertical, null, default, out _));
            Assert.Equal(new[] { new MathGlyphVariant(42, 900) }, variants.GetGlyphVariants(42, MathDirection.Vertical).ToArray());
        }

        [Fact]
        public void Variant_Count_Larger_Than_The_Data_Drops_The_Variants_But_Not_The_Assembly()
        {
            var data = Full().Build();
            Patch(data, ConstructionStart(data, 0) + 2, 0xFFFF);

            var variants = Load(data).Variants!;

            Assert.True(variants.GetGlyphVariants(40, MathDirection.Vertical).IsEmpty);
            Assert.True(variants.TryGetGlyphAssembly(40, MathDirection.Vertical, null, default, out var assembly));
            Assert.Equal(2, assembly.Parts.Length);
        }

        [Fact]
        public void Part_Count_Larger_Than_The_Data_Drops_The_Assembly_But_Not_The_Variants()
        {
            var data = Full().Build();
            var construction = ConstructionStart(data, 0);
            var assembly = construction + U16(data, construction);
            Patch(data, assembly + 4, 0xFFFF);

            var variants = Load(data).Variants!;

            Assert.False(variants.TryGetGlyphAssembly(40, MathDirection.Vertical, null, default, out _));
            Assert.Equal(2, variants.GetGlyphVariants(40, MathDirection.Vertical).Length);
        }

        [Fact]
        public void Construction_Count_Smaller_Than_The_Coverage_Leaves_The_Rest_Uncovered()
        {
            var data = Full().Build();
            Patch(data, VariantsStart(data) + 6, 1);

            var variants = Load(data).Variants!;

            Assert.Equal(2, variants.GetGlyphVariants(40, MathDirection.Vertical).Length);
            Assert.True(variants.GetGlyphVariants(42, MathDirection.Vertical).IsEmpty);
        }

        [Fact]
        public void Construction_Counts_Larger_Than_The_Data_Drop_The_Variants()
        {
            var data = Full().Build();
            Patch(data, VariantsStart(data) + 8, 0xFFFF);

            var table = Load(data);

            Assert.Null(table.Variants);
            Assert.NotNull(table.GlyphInfo);
        }

        [Fact]
        public void Device_Offset_Past_The_End_Or_Of_Another_Format_Leaves_The_Value_Unvaried()
        {
            var inter = SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable).CreateGlyphTypeface();
            Assert.True(GdefTable.TryLoad(inter, 2, out var gdef));
            var store = gdef!.VariationStore!;
            var scalers = new float[store.RegionCount];
            store.ComputeRegionScalers(new[] { 0f, 1f }, scalers);

            var data = Full().Build();
            var italics = ItalicsStart(data);

            // The second italics record (glyph 11): value, then its device offset.
            var deviceOffsetPosition = italics + 4 + 4 + 2;
            var device = italics + U16(data, deviceOffsetPosition);

            Assert.True(Load(data).GlyphInfo!.TryGetItalicsCorrection(11, store, scalers, out var varied));
            Assert.Equal(50 + 70, varied);

            Patch(data, device + 4, 0x0002);
            Assert.True(Load(data).GlyphInfo!.TryGetItalicsCorrection(11, store, scalers, out var otherFormat));
            Assert.Equal(50, otherFormat);

            Patch(data, deviceOffsetPosition, 0xFFF0);
            Assert.True(Load(data).GlyphInfo!.TryGetItalicsCorrection(11, store, scalers, out var outOfRange));
            Assert.Equal(50, outOfRange);
        }

        [Fact]
        public void Variation_Index_Outside_The_Store_Leaves_The_Value_Unvaried()
        {
            var inter = SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable).CreateGlyphTypeface();
            Assert.True(GdefTable.TryLoad(inter, 2, out var gdef));
            var store = gdef!.VariationStore!;
            var scalers = new float[store.RegionCount];
            store.ComputeRegionScalers(new[] { 0f, 1f }, scalers);

            var table = Load(new MathTableBuilder()
                .ItalicsCorrection(1, 10, (0xFFFF, 0xFFFF))
                .ItalicsCorrection(2, 20, (0, 0xFFFF))
                .Build());

            Assert.True(table.GlyphInfo!.TryGetItalicsCorrection(1, store, scalers, out var noVariation));
            Assert.True(table.GlyphInfo!.TryGetItalicsCorrection(2, store, scalers, out var innerOutOfRange));
            Assert.Equal(10, noVariation);
            Assert.Equal(20, innerOutOfRange);
        }

        [Fact]
        public void Every_Truncation_Of_A_Table_Is_Read_Without_Throwing()
        {
            var data = Full().Build();
            var constants = new int[MathTable.ConstantCount];

            for (var length = 0; length <= data.Length; length++)
            {
                if (!MathTable.TryLoad(data.AsMemory(0, length), out var table))
                {
                    continue;
                }

                table.ResolveConstants(constants, null, default);

                foreach (var glyph in new ushort[] { 5, 6, 10, 11, 20, 40, 42 })
                {
                    table.GlyphInfo?.TryGetItalicsCorrection(glyph, null, default, out _);
                    table.GlyphInfo?.TryGetTopAccentAttachment(glyph, null, default, out _);
                    table.GlyphInfo?.IsExtendedShape(glyph);
                    table.GlyphInfo?.GetKerning(glyph, (int)MathKernCorner.TopRight, 150, null, default);
                    table.Variants?.GetGlyphVariants(glyph, MathDirection.Vertical);
                    table.Variants?.TryGetGlyphAssembly(glyph, MathDirection.Vertical, null, default, out _);
                }
            }
        }
    }
}
