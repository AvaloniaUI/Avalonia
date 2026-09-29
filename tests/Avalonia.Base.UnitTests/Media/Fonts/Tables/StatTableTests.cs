using System;
using System.Buffers.Binary;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Media.Fonts.Tables.Variation;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts.Tables
{
    public class StatTableTests
    {
        private const string NotoMonoAsset =
            "resm:Avalonia.Base.UnitTests.Assets.NotoMono-Regular.ttf?assembly=Avalonia.Base.UnitTests";

        private const string AdobeBlankAsset =
            "resm:Avalonia.Base.UnitTests.Assets.AdobeBlank2VF.ttf?assembly=Avalonia.Base.UnitTests";

        [Fact]
        public void Inter_Variable_Declares_Optical_Size_Weight_And_Italic_Axes()
        {
            var stat = SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable).CreateGlyphTypeface().StatTable;

            Assert.NotNull(stat);
            Assert.Equal(
                new[]
                {
                    new StatDesignAxis(OpenTypeTag.Parse("opsz"), 298, 0),
                    new StatDesignAxis(OpenTypeTag.Parse("wght"), 257, 1),
                    new StatDesignAxis(OpenTypeTag.Parse("ital"), 301, 2),
                },
                stat!.DesignAxes);
            Assert.Equal(2, stat.ElidedFallbackNameId);
        }

        [Fact]
        public void Inter_Variable_Axis_Values_Are_Read_In_Table_Order()
        {
            var stat = SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable).CreateGlyphTypeface().StatTable!;

            Assert.Equal(12, stat.AxisValues.Length);

            // "Display" names opsz=32 and is not elidable.
            var display = stat.AxisValues[1];
            Assert.Equal(1, display.Format);
            Assert.Equal(300, display.ValueNameId);
            Assert.Equal(new StatAxisValueRecord(0, 32f), Assert.Single(display.Records));
            Assert.False(display.IsElidable);

            // "Regular" names wght=400, style-links to Bold and is elided from composed names.
            var regular = stat.AxisValues[5];
            Assert.Equal(3, regular.Format);
            Assert.Equal(264, regular.ValueNameId);
            Assert.Equal(new StatAxisValueRecord(1, 400f), Assert.Single(regular.Records));
            Assert.Equal(700f, regular.LinkedValue);
            Assert.True(regular.IsElidable);
            Assert.False(regular.IsOlderSiblingFontAttribute);
        }

        [Fact]
        public void Adobe_Blank_Declares_Axes_Without_Axis_Values()
        {
            var stat = SyntheticFont.FromAsset(AdobeBlankAsset).CreateGlyphTypeface().StatTable!;

            Assert.Equal(2, stat.DesignAxes.Length);
            Assert.Equal(OpenTypeTag.Parse("HGHT"), stat.DesignAxes[1].Tag);
            Assert.Empty(stat.AxisValues);
        }

        [Fact]
        public void Font_Without_STAT_Has_No_Stat_Table()
        {
            var typeface = SyntheticFont.FromAsset(NotoMonoAsset).CreateGlyphTypeface();

            Assert.Null(typeface.StatTable);
        }

        [Fact]
        public void Variation_Clone_Reads_The_Stat_Table_Of_Its_Source()
        {
            var typeface = SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable).CreateGlyphTypeface();
            var clone = typeface.WithVariations(new FontVariationSettings(
                new[] { new FontVariation(OpenTypeTag.Parse("wght"), 700) }));

            Assert.NotSame(typeface, clone);
            Assert.NotNull(clone.StatTable);
            Assert.Same(typeface.StatTable, clone.StatTable);
        }

        [Fact]
        public void Format_1_Names_A_Single_Value()
        {
            var stat = Parse(new StatTableBuilder().Axis("wght", 256, 0).Format1(0, 0, 300, 700));

            var value = Assert.Single(stat.AxisValues);
            Assert.Equal(1, value.Format);
            Assert.Equal(StatAxisValueFlags.None, value.Flags);
            Assert.Equal(300, value.ValueNameId);
            Assert.Equal(new StatAxisValueRecord(0, 700f), Assert.Single(value.Records));
            Assert.Equal(700f, value.RangeMinValue);
            Assert.Equal(700f, value.RangeMaxValue);
            Assert.Null(value.LinkedValue);
        }

        [Fact]
        public void Format_2_Names_A_Range_Around_A_Nominal_Value()
        {
            var stat = Parse(new StatTableBuilder().Axis("opsz", 256, 0).Format2(0, 0, 301, 12, 9.5, 18.5));

            var value = Assert.Single(stat.AxisValues);
            Assert.Equal(2, value.Format);
            Assert.Equal(new StatAxisValueRecord(0, 12f), Assert.Single(value.Records));
            Assert.Equal(9.5f, value.RangeMinValue);
            Assert.Equal(18.5f, value.RangeMaxValue);
            Assert.Null(value.LinkedValue);
        }

        [Fact]
        public void Format_3_Names_A_Value_With_Its_Style_Link()
        {
            var stat = Parse(new StatTableBuilder().Axis("wght", 256, 0).Format3(0, 2, 302, 400, 700));

            var value = Assert.Single(stat.AxisValues);
            Assert.Equal(3, value.Format);
            Assert.Equal(StatAxisValueFlags.ElidableAxisValueName, value.Flags);
            Assert.Equal(new StatAxisValueRecord(0, 400f), Assert.Single(value.Records));
            Assert.Equal(700f, value.LinkedValue);
        }

        [Fact]
        public void Format_4_Names_A_Combination_Of_Axis_Values()
        {
            var stat = Parse(new StatTableBuilder()
                .Axis("wght", 256, 0)
                .Axis("wdth", 257, 1)
                .Format4(0, 303, (0, 600), (1, 75)));

            var value = Assert.Single(stat.AxisValues);
            Assert.Equal(4, value.Format);
            Assert.Equal(303, value.ValueNameId);
            Assert.Equal(
                new[] { new StatAxisValueRecord(0, 600f), new StatAxisValueRecord(1, 75f) },
                value.Records);
            Assert.Null(value.LinkedValue);
        }

        [Fact]
        public void Older_Sibling_Font_Attribute_Flag_Is_Read()
        {
            var stat = Parse(new StatTableBuilder().Axis("wdth", 256, 0).Format1(0, 1, 300, 100));

            var value = Assert.Single(stat.AxisValues);
            Assert.True(value.IsOlderSiblingFontAttribute);
            Assert.False(value.IsElidable);
        }

        [Fact]
        public void Version_1_0_Falls_Back_To_Name_Id_2_For_Elided_Names()
        {
            var data = new StatTableBuilder { MinorVersion = 0 }.Axis("wght", 256, 0).Format1(0, 0, 300, 700).Build();

            var stat = StatTable.TryParse(data);

            Assert.NotNull(stat);
            Assert.Equal(2, stat!.ElidedFallbackNameId);
            Assert.Single(stat.AxisValues);
        }

        [Fact]
        public void Version_1_1_Reads_The_Elided_Fallback_Name_Id()
        {
            var stat = Parse(new StatTableBuilder { MinorVersion = 1, ElidedFallbackNameId = 310 }.Axis("wght", 256, 0));

            Assert.Equal(310, stat.ElidedFallbackNameId);
        }

        [Fact]
        public void Larger_Design_Axis_Records_Are_Stepped_Over()
        {
            var stat = Parse(new StatTableBuilder { DesignAxisSize = 12 }
                .Axis("wght", 256, 1)
                .Axis("opsz", 257, 0));

            Assert.Equal(new StatDesignAxis(OpenTypeTag.Parse("opsz"), 257, 0), stat.DesignAxes[1]);
        }

        [Fact]
        public void Unknown_Axis_Value_Format_Is_Skipped()
        {
            var stat = Parse(new StatTableBuilder()
                .Axis("wght", 256, 0)
                .Value(new BigEndianBuffer().UInt16(5).UInt16(0).UInt16(0).UInt16(0).ToArray())
                .Format1(0, 0, 300, 700));

            var value = Assert.Single(stat.AxisValues);
            Assert.Equal(300, value.ValueNameId);
        }

        [Fact]
        public void Unsupported_Major_Version_Is_Rejected()
        {
            var data = new StatTableBuilder().Axis("wght", 256, 0).Build();
            BinaryPrimitives.WriteUInt16BigEndian(data, 2);

            Assert.Null(StatTable.TryParse(data));
        }

        [Fact]
        public void Every_Truncation_Is_Rejected_Without_Throwing()
        {
            var data = new StatTableBuilder()
                .Axis("wght", 256, 0)
                .Axis("wdth", 257, 1)
                .Format1(0, 0, 300, 700)
                .Format2(1, 0, 301, 75, 70, 80)
                .Format3(0, 2, 302, 400, 700)
                .Format4(0, 303, (0, 600), (1, 75))
                .Build();

            Assert.NotNull(StatTable.TryParse(data));

            for (var length = 0; length < data.Length; length++)
            {
                Assert.Null(StatTable.TryParse(data.AsSpan(0, length)));
            }
        }

        [Fact]
        public void Design_Axes_Offset_Past_The_Table_End_Is_Rejected()
        {
            var data = new StatTableBuilder().Axis("wght", 256, 0).Build();
            BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(8), (uint)data.Length - 4);

            Assert.Null(StatTable.TryParse(data));
        }

        [Fact]
        public void Design_Axes_Offset_With_The_Sign_Bit_Set_Is_Rejected()
        {
            var data = new StatTableBuilder().Axis("wght", 256, 0).Build();
            BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(8), 0x8000_0000);

            Assert.Null(StatTable.TryParse(data));
        }

        [Fact]
        public void Design_Axis_Size_Below_The_Record_Size_Is_Rejected()
        {
            var data = new StatTableBuilder().Axis("wght", 256, 0).Build();
            BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(4), 6);

            Assert.Null(StatTable.TryParse(data));
        }

        [Fact]
        public void Design_Axis_Count_Beyond_The_Data_Is_Rejected()
        {
            var data = new StatTableBuilder().Axis("wght", 256, 0).Build();
            BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(6), ushort.MaxValue);

            Assert.Null(StatTable.TryParse(data));
        }

        [Fact]
        public void Axis_Value_Count_Beyond_The_Data_Is_Rejected()
        {
            var data = new StatTableBuilder().Axis("wght", 256, 0).Format1(0, 0, 300, 700).Build();
            BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(12), ushort.MaxValue);

            Assert.Null(StatTable.TryParse(data));
        }

        [Fact]
        public void Axis_Value_Offset_Past_The_Table_End_Is_Rejected()
        {
            var data = new StatTableBuilder().Axis("wght", 256, 0).Format1(0, 0, 300, 700).Build();
            var arrayStart = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(14));
            BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(arrayStart), ushort.MaxValue);

            Assert.Null(StatTable.TryParse(data));
        }

        [Fact]
        public void Axis_Value_Referencing_A_Missing_Design_Axis_Is_Rejected()
        {
            var data = new StatTableBuilder().Axis("wght", 256, 0).Format1(1, 0, 300, 700).Build();

            Assert.Null(StatTable.TryParse(data));
        }

        [Fact]
        public void Format_4_Axis_Count_Beyond_The_Data_Is_Rejected()
        {
            var format4 = new BigEndianBuffer()
                .UInt16(4).UInt16(ushort.MaxValue).UInt16(0).UInt16(303).UInt16(0).Fixed(600)
                .ToArray();
            var data = new StatTableBuilder().Axis("wght", 256, 0).Value(format4).Build();

            Assert.Null(StatTable.TryParse(data));
        }

        [Fact]
        public void Format_4_Record_Referencing_A_Missing_Design_Axis_Is_Rejected()
        {
            var data = new StatTableBuilder().Axis("wght", 256, 0).Format4(0, 303, (0, 600), (3, 75)).Build();

            Assert.Null(StatTable.TryParse(data));
        }

        [Fact]
        public void Malformed_STAT_Does_Not_Deny_The_Font()
        {
            var font = SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable).Truncate("STAT", 30);

            var typeface = font.TryCreateGlyphTypeface();

            Assert.NotNull(typeface);
            Assert.NotEmpty(typeface!.VariationAxes);
            Assert.Null(typeface.StatTable);
        }

        private static StatTable Parse(StatTableBuilder builder)
        {
            var stat = StatTable.TryParse(builder.Build());

            Assert.NotNull(stat);

            return stat!;
        }
    }
}
