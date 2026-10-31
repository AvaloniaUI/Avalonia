using System;
using Avalonia.Media;
using Avalonia.Media.Fonts.Tables.Variation;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts.Tables
{
    public class AvarTableTests
    {
        private const string InterRegularAsset =
            "resm:Avalonia.Base.UnitTests.Assets.Inter-Regular.ttf?assembly=Avalonia.Base.UnitTests";

        private const string InterVariableAsset =
            "resm:Avalonia.Base.UnitTests.Assets.InterVariable.ttf?assembly=Avalonia.Base.UnitTests";

        private static GlyphTypeface LoadTypeface(string assetUri)
        {
            var assetLoader = new StandardAssetLoader();
            using var stream = assetLoader.Open(new Uri(assetUri));
            return new GlyphTypeface(new CustomPlatformTypeface(stream));
        }

        [Fact]
        public void TryLoad_Returns_False_For_Static_Font()
        {
            var typeface = LoadTypeface(InterRegularAsset);

            Assert.False(AvarTable.TryLoad(typeface, out var avar));
            Assert.Null(avar);
        }

        [Fact]
        public void TryLoad_Returns_True_For_Inter_Variable()
        {
            var typeface = LoadTypeface(InterVariableAsset);

            Assert.True(AvarTable.TryLoad(typeface, out var avar));
            Assert.NotNull(avar);
            Assert.Equal(2, avar!.AxisCount);
        }

        [Fact]
        public void Remap_Is_Identity_For_Linear_Axis()
        {
            // Inter Variable's opsz axis has an identity-only segment map:
            //   (-1 → -1), (0 → 0), (+1 → +1). Any input passes through unchanged.
            var typeface = LoadTypeface(InterVariableAsset);
            Assert.True(AvarTable.TryLoad(typeface, out var avar));

            Assert.Equal(-1f, avar!.Remap(axisIndex: 0, -1f));
            Assert.Equal(0f, avar.Remap(axisIndex: 0, 0f));
            Assert.Equal(0.5f, avar.Remap(axisIndex: 0, 0.5f));
            Assert.Equal(1f, avar.Remap(axisIndex: 0, 1f));
        }

        [Fact]
        public void Remap_Applies_Bend_For_NonLinear_Axis()
        {
            // Inter Variable's wght axis bends through these segment-map entries:
            //   (-1 → -1), (0 → 0), (0.2 → 0.18), (0.4 → 0.36),
            //   (0.6 → 0.54), (0.8 → 0.76), (1.0 → 1.0)
            // Each exact entry must round-trip; intermediate values are linear between
            // the surrounding entries.
            var typeface = LoadTypeface(InterVariableAsset);
            Assert.True(AvarTable.TryLoad(typeface, out var avar));

            const float tolerance = 1e-4f;

            Assert.Equal(0.18f, avar!.Remap(axisIndex: 1, 0.2f), tolerance);
            Assert.Equal(0.36f, avar.Remap(axisIndex: 1, 0.4f), tolerance);
            Assert.Equal(0.54f, avar.Remap(axisIndex: 1, 0.6f), tolerance);
            Assert.Equal(0.76f, avar.Remap(axisIndex: 1, 0.8f), tolerance);

            // Midpoint between (0.4 → 0.36) and (0.6 → 0.54) should be (0.5 → 0.45) under
            // linear interpolation.
            Assert.Equal(0.45f, avar.Remap(axisIndex: 1, 0.5f), tolerance);
        }

        [Fact]
        public void Remap_Clamps_Out_Of_Range_Input()
        {
            // The avar domain is exactly [-1, 1]; values outside that range are clamped
            // before lookup. This prevents extrapolation off the ends of the segment list.
            var typeface = LoadTypeface(InterVariableAsset);
            Assert.True(AvarTable.TryLoad(typeface, out var avar));

            Assert.Equal(-1f, avar!.Remap(axisIndex: 1, -2f));
            Assert.Equal(1f, avar.Remap(axisIndex: 1, 5f));
        }

        [Fact]
        public void Remap_Is_Identity_For_Out_Of_Range_Axis_Index()
        {
            // Defensive: an axis index beyond the table's axisCount returns the input
            // unchanged. This matters if a font's avar declares fewer axes than fvar,
            // which the spec forbids but malformed fonts sometimes do.
            var typeface = LoadTypeface(InterVariableAsset);
            Assert.True(AvarTable.TryLoad(typeface, out var avar));

            Assert.Equal(0.5f, avar!.Remap(axisIndex: 99, 0.5f));
            Assert.Equal(0.5f, avar.Remap(axisIndex: -1, 0.5f));
        }

        [Theory]
        [InlineData(0, 0.5f)]
        [InlineData(1, 0.75f)]
        [InlineData(2, -0.5f)]
        [InlineData(3, 0.5f)]
        public void Remap_Is_Identity_For_Segment_Map_Missing_A_Required_Identity_Entry(
            int axisIndex, float coordinate)
        {
            // A segment map with any entries must contain -1 -> -1, 0 -> 0 and +1 -> +1. When one of
            // them is missing the spec leaves that axis unmodified.
            var avar = new BigEndianBuffer()
                .UInt16(1)  // majorVersion
                .UInt16(0)  // minorVersion
                .UInt16(0)  // reserved
                .UInt16(4); // axisCount

            // Axis 0: no 0 -> 0 entry.
            WriteSegmentMap(avar, (-1, -1), (0.5, 0.25), (1, 1));

            // Axis 1: no +1 -> +1 entry.
            WriteSegmentMap(avar, (-1, -1), (0, 0), (0.5, 0.25));

            // Axis 2: no -1 -> -1 entry.
            WriteSegmentMap(avar, (-0.5, -0.25), (0, 0), (1, 1));

            // Axis 3: +1 is present but not mapped to itself.
            WriteSegmentMap(avar, (-1, -1), (0, 0), (0.5, 0.25), (1, 0.75));

            var typeface = SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable)
                .Replace("avar", avar.ToArray())
                .TryCreateGlyphTypeface();

            Assert.NotNull(typeface);
            Assert.True(AvarTable.TryLoad(typeface!, out var table));

            Assert.Equal(coordinate, table!.Remap(axisIndex, coordinate));
        }

        [Theory]
        [InlineData(0f, 0f)]          // the plateau at 0 contains the default, which stays put
        [InlineData(-0.5f, -0.75f)]   // inside the first segment
        [InlineData(0.5f, 0.5f)]
        public void Unmap_Keeps_The_Default_On_A_Plateau_Through_Zero(float mapped, float expected)
        {
            // The spec allows equal toCoordinates: -0.5 .. 0 all map to 0.
            var table = LoadWithWeightMap((-1, -1), (-0.5, 0), (0, 0), (1, 1));

            Assert.Equal(expected, table.Unmap(1, mapped), 1e-4f);
        }

        [Fact]
        public void Unmap_Picks_The_Plateau_Input_Closest_To_The_Default()
        {
            // -0.5 .. -0.25 all map to -0.5; -0.25 moves least from the default.
            var table = LoadWithWeightMap((-1, -1), (-0.5, -0.5), (-0.25, -0.5), (0, 0), (1, 1));

            Assert.Equal(-0.25f, table.Unmap(1, -0.5f), 1e-4f);
        }

        private static AvarTable LoadWithWeightMap(params (double From, double To)[] weightMap)
        {
            var typeface = SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable)
                .Replace("avar", BuildInterAvar(weightMap))
                .TryCreateGlyphTypeface();

            Assert.NotNull(typeface);
            Assert.True(AvarTable.TryLoad(typeface!, out var table));

            return table!;
        }

        /// <summary>
        /// Builds an avar table for Inter Variable's opsz (axis 0, identity) and wght (axis 1)
        /// axes with <paramref name="weightMap"/> as the wght segment map.
        /// </summary>
        internal static byte[] BuildInterAvar(params (double From, double To)[] weightMap)
        {
            var avar = new BigEndianBuffer()
                .UInt16(1)  // majorVersion
                .UInt16(0)  // minorVersion
                .UInt16(0)  // reserved
                .UInt16(2); // axisCount

            WriteSegmentMap(avar, (-1, -1), (0, 0), (1, 1));
            WriteSegmentMap(avar, weightMap);

            return avar.ToArray();
        }

        private static void WriteSegmentMap(BigEndianBuffer buffer, params (double From, double To)[] entries)
        {
            buffer.UInt16(entries.Length);

            foreach (var (from, to) in entries)
            {
                buffer.F2Dot14(from).F2Dot14(to);
            }
        }
    }
}
