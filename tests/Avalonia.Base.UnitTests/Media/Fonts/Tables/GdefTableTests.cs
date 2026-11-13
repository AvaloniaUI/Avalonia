using System;
using System.Buffers.Binary;
using Avalonia.Media.Fonts.Tables;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts.Tables
{
    public class GdefTableTests
    {
        // InterVariable has the axes opsz and wght; its GDEF 1.3 store has 24 delta set groups
        // over 5 regions (fontTools: GDEF.VarStore).
        private const int InterAxisCount = 2;

        private static byte[] InterGdef() => SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable).GetTable("GDEF");

        private static byte[] InterStore()
        {
            var gdef = InterGdef();
            var storeOffset = (int)BinaryPrimitives.ReadUInt32BigEndian(gdef.AsSpan(14));

            return gdef.AsSpan(storeOffset).ToArray();
        }

        private static BigEndianBuffer Header(int minorVersion)
            => new BigEndianBuffer().UInt16(1).UInt16(minorVersion).UInt16(0).UInt16(0).UInt16(0).UInt16(0);

        [Fact]
        public void Inter_Variable_Store_Is_Read()
        {
            var typeface = SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable).CreateGlyphTypeface();

            Assert.True(GdefTable.TryLoad(typeface, InterAxisCount, out var gdef));
            Assert.Equal(3, gdef!.MinorVersion);
            Assert.NotNull(gdef.VariationStore);
            Assert.Equal(5, gdef.VariationStore!.RegionCount);
            Assert.Equal(24, gdef.VariationStore.ItemVariationDataCount);
        }

        [Fact]
        public void Font_Without_GDEF_Has_No_Table()
        {
            var typeface = SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable).Remove("GDEF").CreateGlyphTypeface();

            Assert.False(GdefTable.TryLoad(typeface, InterAxisCount, out _));
        }

        [Fact]
        public void Version_1_0_And_1_2_Have_No_Store()
        {
            Assert.True(GdefTable.TryLoad(Header(0).ToArray(), InterAxisCount, out var version10));
            Assert.Null(version10!.VariationStore);

            Assert.True(GdefTable.TryLoad(Header(2).UInt16(0).ToArray(), InterAxisCount, out var version12));
            Assert.Equal(2, version12!.MinorVersion);
            Assert.Null(version12.VariationStore);
        }

        [Fact]
        public void Version_1_3_Reads_The_Store_At_Its_Offset()
        {
            var data = Header(3).UInt16(0).UInt32(18).Bytes(InterStore()).ToArray();

            Assert.True(GdefTable.TryLoad(data, InterAxisCount, out var gdef));
            Assert.Equal(5, gdef!.VariationStore!.RegionCount);
        }

        [Fact]
        public void Version_1_3_Without_A_Store_Offset_Has_No_Store()
        {
            Assert.True(GdefTable.TryLoad(Header(3).UInt16(0).UInt32(0).ToArray(), InterAxisCount, out var gdef));
            Assert.Null(gdef!.VariationStore);
        }

        [Fact]
        public void Version_1_3_With_A_Short_Header_Has_No_Store()
        {
            Assert.True(GdefTable.TryLoad(Header(3).UInt16(0).ToArray(), InterAxisCount, out var gdef));
            Assert.Null(gdef!.VariationStore);
        }

        [Fact]
        public void Store_Offset_Past_The_Table_Leaves_No_Store()
        {
            var data = Header(3).UInt16(0).UInt32(0xFFFFFFF0).ToArray();

            Assert.True(GdefTable.TryLoad(data, InterAxisCount, out var gdef));
            Assert.Null(gdef!.VariationStore);
        }

        [Fact]
        public void Store_With_A_Different_Axis_Count_Is_Dropped()
        {
            var data = Header(3).UInt16(0).UInt32(18).Bytes(InterStore()).ToArray();

            Assert.True(GdefTable.TryLoad(data, InterAxisCount + 1, out var gdef));
            Assert.Null(gdef!.VariationStore);
        }

        [Fact]
        public void Unsupported_Major_Version_Or_Short_Header_Is_Rejected()
        {
            Assert.False(GdefTable.TryLoad(new BigEndianBuffer().UInt16(2).UInt16(0).Zeros(8).ToArray(), InterAxisCount, out _));
            Assert.False(GdefTable.TryLoad(Header(0).ToArray().AsMemory(0, 11), InterAxisCount, out _));
        }
    }
}
