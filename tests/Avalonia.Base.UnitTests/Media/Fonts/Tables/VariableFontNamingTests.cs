using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Media.Fonts.Tables.Variation;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts.Tables
{
    public class VariableFontNamingTests
    {
        private const string AdobeBlankAsset =
            "resm:Avalonia.Base.UnitTests.Assets.AdobeBlank2VF.ttf?assembly=Avalonia.Base.UnitTests";

        private const int Elidable = 2;
        private const int OlderSibling = 1;

        private static readonly OpenTypeTag s_wght = OpenTypeTag.Parse("wght");
        private static readonly OpenTypeTag s_wdth = OpenTypeTag.Parse("wdth");
        private static readonly OpenTypeTag s_opsz = OpenTypeTag.Parse("opsz");
        private static readonly OpenTypeTag s_grad = OpenTypeTag.Parse("GRAD");

        private static readonly Dictionary<int, string> s_names = new()
        {
            [2] = "Regular",
            [300] = "Regular",
            [301] = "Bold",
            [302] = "Condensed",
            [303] = "Normal",
            [304] = "Text",
            [305] = "Display",
            [306] = "Italic",
            [307] = "Roman",
            [308] = "Heavy Compressed",
            [309] = "Grade High",
            [310] = "Book",
        };

        private static readonly FontVariationAxis[] s_axes =
        {
            new(s_wght, "Weight", 100, 400, 900, false),
            new(s_wdth, "Width", 75, 100, 100, false),
            new(s_opsz, "Optical size", 8, 12, 48, false),
        };

        [Fact]
        public void Inter_Variable_Default_Instance_Is_Regular_In_The_Text_Family()
        {
            var typeface = SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable).CreateGlyphTypeface();

            Assert.True(typeface.TryGetInstanceNames(default(NormalizedVariationPosition), out var names));

            // "Text" names opsz=14 and is not elidable, so it joins the family like
            // DirectWrite's "Segoe UI Variable Text"; Regular and Roman are elided.
            Assert.Equal(new FontInstanceNames("Inter Variable Text", "Regular"), names);
        }

        [Fact]
        public void Inter_Variable_Optical_Size_Selects_The_Family_And_Weight_The_Style()
        {
            var typeface = SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable).CreateGlyphTypeface();
            var clone = typeface.WithVariations(new FontVariationSettings(new[]
            {
                new FontVariation(s_opsz, 32),
                new FontVariation(s_wght, 600),
            }));

            Assert.True(clone.TryGetInstanceNames(clone.VariationPosition, out var names));

            Assert.Equal(new FontInstanceNames("Inter Variable Display", "SemiBold"), names);
        }

        [Fact]
        public void Inter_Variable_Named_Instances_Keep_Their_Style_Names()
        {
            var typeface = SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable).CreateGlyphTypeface();

            Assert.Equal(9, typeface.NamedInstances.Count);

            foreach (var instance in typeface.NamedInstances)
            {
                var position = typeface.CreateNormalizedPosition(null, instance.Index);

                Assert.True(typeface.TryGetInstanceNames(position, out var fromPosition));
                Assert.True(typeface.TryGetInstanceNames(instance.Coordinates, out var fromCoordinates));

                Assert.Equal(new FontInstanceNames("Inter Variable Text", instance.Name), fromPosition);
                Assert.Equal(fromPosition, fromCoordinates);
            }
        }

        [Fact]
        public void Font_With_STAT_Axes_But_No_Axis_Values_Takes_Named_Instance_Names()
        {
            var typeface = SyntheticFont.FromAsset(AdobeBlankAsset).CreateGlyphTypeface();

            Assert.NotNull(typeface.StatTable);
            Assert.True(typeface.TryGetInstanceNames(default(NormalizedVariationPosition), out var names));
            Assert.Equal(new FontInstanceNames("Adobe Blank 2 VF", "Regular"), names);

            var clone = typeface.WithVariations(Settings(s_wdth, 500));

            Assert.False(clone.TryGetInstanceNames(clone.VariationPosition, out _));
        }

        [Fact]
        public void Font_Without_STAT_Takes_Named_Instance_Names()
        {
            var typeface = SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable).Remove("STAT")
                .CreateGlyphTypeface();

            Assert.Null(typeface.StatTable);

            var bold = typeface.CreateNormalizedPosition(Settings(s_wght, 700));

            Assert.True(typeface.TryGetInstanceNames(bold, out var names));
            Assert.Equal(new FontInstanceNames("Inter Variable", "Bold"), names);

            var between = typeface.CreateNormalizedPosition(Settings(s_wght, 450));

            Assert.False(typeface.TryGetInstanceNames(between, out _));
        }

        [Fact]
        public void Static_Font_Has_No_Instance_Names()
        {
            var typeface = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular).CreateGlyphTypeface();

            Assert.False(typeface.TryGetInstanceNames(default(NormalizedVariationPosition), out _));
        }

        [Fact]
        public void Style_Names_Follow_The_Axis_Ordering()
        {
            var builder = new StatTableBuilder()
                .Axis("wght", 256, 1)
                .Axis("wdth", 257, 0)
                .Format1(0, 0, 301, 700)
                .Format1(1, 0, 302, 75);

            var names = Names(builder, (s_wght, 700), (s_wdth, 75));

            Assert.Equal(new FontInstanceNames("Family", "Condensed Bold"), names);
        }

        [Fact]
        public void Names_Of_Other_Axes_Join_The_Family()
        {
            var builder = new StatTableBuilder()
                .Axis("wght", 256, 0)
                .Axis("opsz", 257, 1)
                .Format1(0, 0, 301, 700)
                .Format1(1, 0, 305, 48);

            var names = Names(builder, (s_wght, 700), (s_opsz, 48));

            Assert.Equal(new FontInstanceNames("Family Display", "Bold"), names);
        }

        [Fact]
        public void Custom_Axis_Names_Join_The_Family_In_Axis_Order()
        {
            var axes = new List<FontVariationAxis>(s_axes) { new(s_grad, "Grade", 0, 0, 100, false) };
            var builder = new StatTableBuilder()
                .Axis("GRAD", 258, 1)
                .Axis("opsz", 257, 0)
                .Format1(0, 0, 309, 100)
                .Format1(1, 0, 305, 48);

            var names = Names(builder, axes, (s_grad, 100), (s_opsz, 48));

            Assert.Equal(new FontInstanceNames("Family Display Grade High", "Regular"), names);
        }

        [Fact]
        public void Family_Positions_Combine_The_Named_Values_Of_The_Non_Style_Axes()
        {
            var axes = new List<FontVariationAxis>(s_axes) { new(s_grad, "Grade", 0, 0, 100, false) };
            var builder = new StatTableBuilder()
                .Axis("wght", 256, 0)
                .Axis("opsz", 257, 1)
                .Axis("GRAD", 258, 2)
                .Format1(0, 0, 301, 700)
                .Format1(1, 0, 304, 12)
                .Format2(1, 0, 305, 48, 20, 48)
                .Format1(1, 0, 305, 96)
                .Format1(2, 0, 309, 100)
                .Format4(0, 308, (0, 700), (1, 48));

            var positions = VariableFontNaming.GetFamilyPositions(StatTable.TryParse(builder.Build()), axes);

            // wght is a style axis, opsz 96 lies outside the axis range and format 4 combinations
            // name no family on their own.
            Assert.Equal(
                new[] { "opsz=12,GRAD=100", "opsz=48,GRAD=100" },
                positions.Select(x => $"opsz={x[s_opsz]},GRAD={x[s_grad]}"));
            Assert.All(positions, x => Assert.Equal(2, x.Count));
        }

        [Fact]
        public void Font_Without_STAT_Has_No_Family_Positions()
        {
            Assert.Empty(VariableFontNaming.GetFamilyPositions(null, s_axes));
        }

        [Fact]
        public void Elidable_Names_Are_Dropped()
        {
            var builder = new StatTableBuilder()
                .Axis("wght", 256, 0)
                .Axis("wdth", 257, 1)
                .Format1(0, 0, 301, 700)
                .Format1(1, Elidable, 303, 100);

            var names = Names(builder, (s_wght, 700));

            Assert.Equal(new FontInstanceNames("Family", "Bold"), names);
        }

        [Fact]
        public void Elided_Fallback_Name_Replaces_A_Fully_Elided_Style()
        {
            var builder = new StatTableBuilder { ElidedFallbackNameId = 310 }
                .Axis("wght", 256, 0)
                .Axis("opsz", 257, 1)
                .Format3(0, Elidable, 300, 400, 700)
                .Format1(1, 0, 305, 48);

            var names = Names(builder, (s_opsz, 48));

            Assert.Equal(new FontInstanceNames("Family Display", "Book"), names);
        }

        [Fact]
        public void Format_2_Range_Names_Every_Value_It_Contains()
        {
            var builder = new StatTableBuilder()
                .Axis("opsz", 257, 0)
                .Format2(0, 0, 304, 12, 8, 20)
                .Format2(0, 0, 305, 36, 20, 48);

            Assert.Equal("Family Text", Names(builder, (s_opsz, 9)).FamilyName);
            Assert.Equal("Family Display", Names(builder, (s_opsz, 40)).FamilyName);
        }

        [Fact]
        public void Shared_Range_Boundary_Takes_The_Nearest_Nominal_Value()
        {
            var builder = new StatTableBuilder()
                .Axis("opsz", 257, 0)
                .Format2(0, 0, 304, 12, 8, 20)
                .Format2(0, 0, 305, 24, 20, 48);

            Assert.Equal("Family Display", Names(builder, (s_opsz, 20)).FamilyName);
        }

        [Fact]
        public void Format_1_Value_Must_Match_Exactly()
        {
            var builder = new StatTableBuilder()
                .Axis("wght", 256, 0)
                .Format1(0, 0, 301, 700);

            Assert.Equal(new FontInstanceNames("Family", "Regular"), Names(builder, (s_wght, 650)));
        }

        [Fact]
        public void Format_4_Combination_Replaces_The_Single_Axis_Names()
        {
            var builder = new StatTableBuilder()
                .Axis("wght", 256, 0)
                .Axis("wdth", 257, 1)
                .Format1(0, 0, 301, 700)
                .Format1(1, 0, 302, 75)
                .Format4(0, 308, (0, 700), (1, 75));

            Assert.Equal(
                new FontInstanceNames("Family", "Heavy Compressed"),
                Names(builder, (s_wght, 700), (s_wdth, 75)));
            Assert.Equal(new FontInstanceNames("Family", "Bold"), Names(builder, (s_wght, 700)));
        }

        [Fact]
        public void Format_4_Combination_With_Another_Axis_Joins_The_Family()
        {
            var builder = new StatTableBuilder()
                .Axis("wght", 256, 0)
                .Axis("opsz", 257, 1)
                .Format1(0, 0, 301, 700)
                .Format4(0, 308, (0, 700), (1, 48));

            Assert.Equal(
                new FontInstanceNames("Family Heavy Compressed", "Regular"),
                Names(builder, (s_wght, 700), (s_opsz, 48)));
        }

        [Fact]
        public void Older_Sibling_Font_Attributes_Do_Not_Name_The_Instance()
        {
            var builder = new StatTableBuilder()
                .Axis("wght", 256, 0)
                .Axis("wdth", 257, 1)
                .Format1(0, 0, 301, 700)
                .Format1(1, OlderSibling, 303, 100);

            Assert.Equal(new FontInstanceNames("Family", "Bold"), Names(builder, (s_wght, 700)));
        }

        [Fact]
        public void Axis_The_Font_Does_Not_Vary_Takes_Its_Declared_Value()
        {
            // An italic companion font of a family whose ital axis is not an fvar axis.
            var builder = new StatTableBuilder()
                .Axis("wght", 256, 0)
                .Axis("ital", 258, 1)
                .Format1(0, 0, 301, 700)
                .Format1(1, 0, 306, 1);

            Assert.Equal(new FontInstanceNames("Family", "Bold Italic"), Names(builder, (s_wght, 700)));
        }

        [Fact]
        public void Value_Without_A_Name_Record_Is_Skipped()
        {
            var builder = new StatTableBuilder()
                .Axis("wght", 256, 0)
                .Axis("opsz", 257, 1)
                .Format1(0, 0, 301, 700)
                .Format1(1, 0, 999, 48);

            Assert.Equal(new FontInstanceNames("Family", "Bold"), Names(builder, (s_wght, 700), (s_opsz, 48)));
        }

        [Fact]
        public void Without_STAT_A_Named_Instance_Is_Named_By_Its_Subfamily_Name_Id()
        {
            var instances = new[]
            {
                new FontVariationInstance(string.Empty, 0, Coordinates((s_wght, 400)), null, 300),
                new FontVariationInstance(string.Empty, 1, Coordinates((s_wght, 700)), null, 301),
            };

            Assert.True(VariableFontNaming.TryGetInstanceNames(
                null, s_axes, instances, "Family", Coordinates((s_wght, 700)), GetName, out var names));
            Assert.Equal(new FontInstanceNames("Family", "Bold"), names);

            Assert.False(VariableFontNaming.TryGetInstanceNames(
                null, s_axes, instances, "Family", Coordinates((s_wdth, 75)), GetName, out _));
        }

        private static FontInstanceNames Names(
            StatTableBuilder builder,
            params (OpenTypeTag Tag, float Value)[] coordinates)
            => Names(builder, s_axes, coordinates);

        private static FontInstanceNames Names(
            StatTableBuilder builder,
            IReadOnlyList<FontVariationAxis> axes,
            params (OpenTypeTag Tag, float Value)[] coordinates)
        {
            var stat = StatTable.TryParse(builder.Build());

            Assert.NotNull(stat);
            Assert.True(VariableFontNaming.TryGetInstanceNames(
                stat,
                axes,
                Array.Empty<FontVariationInstance>(),
                "Family",
                Coordinates(coordinates),
                GetName,
                out var names));

            return names;
        }

        private static Dictionary<OpenTypeTag, float> Coordinates(params (OpenTypeTag Tag, float Value)[] coordinates)
        {
            var result = new Dictionary<OpenTypeTag, float>();

            foreach (var (tag, value) in coordinates)
            {
                result[tag] = value;
            }

            return result;
        }

        private static FontVariationSettings Settings(OpenTypeTag tag, double value)
            => new(new[] { new FontVariation(tag, value) });

        private static string? GetName(ushort nameId) => s_names.TryGetValue(nameId, out var name) ? name : null;
    }
}
