using System;
using System.Collections.Generic;
using Avalonia.Base.UnitTests.Media.Fonts.Tables;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class GlyphTypefaceVariationTests
    {
        private const string InterRegularAsset =
            "resm:Avalonia.Base.UnitTests.Assets.Inter-Regular.ttf?assembly=Avalonia.Base.UnitTests";

        private const string InterVariableAsset =
            "resm:Avalonia.Base.UnitTests.Assets.InterVariable.ttf?assembly=Avalonia.Base.UnitTests";

        private static readonly OpenTypeTag s_wghtTag = OpenTypeTag.Parse("wght");
        private static readonly OpenTypeTag s_opszTag = OpenTypeTag.Parse("opsz");

        private static GlyphTypeface LoadTypeface(string assetUri)
        {
            var assetLoader = new StandardAssetLoader();
            using var stream = assetLoader.Open(new Uri(assetUri));
            return new GlyphTypeface(new CustomPlatformTypeface(stream));
        }

        [Fact]
        public void VariationAxes_Is_Empty_For_Static_Font()
        {
            var typeface = LoadTypeface(InterRegularAsset);

            Assert.Empty(typeface.VariationAxes);
            Assert.Empty(typeface.NamedInstances);
        }

        [Fact]
        public void VariationAxes_Reports_Inter_Variable_Axes()
        {
            var typeface = LoadTypeface(InterVariableAsset);

            Assert.Equal(2, typeface.VariationAxes.Count);
            Assert.Equal(s_opszTag, typeface.VariationAxes[0].Tag);
            Assert.Equal(s_wghtTag, typeface.VariationAxes[1].Tag);
        }

        [Fact]
        public void NamedInstances_Reports_All_Nine_Presets()
        {
            var typeface = LoadTypeface(InterVariableAsset);

            Assert.Equal(9, typeface.NamedInstances.Count);
        }

        [Fact]
        public void CreateVariationSettings_Returns_Default_For_Static_Font()
        {
            // Static fonts have no axes to normalize — every call returns the canonical
            // "no variation" value regardless of the requested coordinates.
            var typeface = LoadTypeface(InterRegularAsset);

            var settings = typeface.CreateVariationSettings(
                new Dictionary<OpenTypeTag, float> { [s_wghtTag] = 700f });

            Assert.True(settings.IsDefault);
        }

        [Fact]
        public void CreateVariationSettings_Returns_Default_For_Axis_At_Default()
        {
            // Asking for the default-instance point yields the canonical default settings.
            // Identity here matters for the FontCollection cache contract in pr4b — two
            // requests for the default instance must produce equal settings values.
            var typeface = LoadTypeface(InterVariableAsset);

            var settings = typeface.CreateVariationSettings(
                new Dictionary<OpenTypeTag, float>
                {
                    [s_wghtTag] = 400f,  // = default
                    [s_opszTag] = 14f,   // = default
                });

            Assert.True(settings.IsDefault);
        }

        [Fact]
        public void CreateVariationSettings_Returns_Default_When_No_Input_Provided()
        {
            // Null / empty input + no instance index = all axes at default = default settings.
            var typeface = LoadTypeface(InterVariableAsset);

            Assert.True(typeface.CreateVariationSettings(null).IsDefault);
            Assert.True(typeface.CreateVariationSettings(new Dictionary<OpenTypeTag, float>()).IsDefault);
        }

        [Fact]
        public void CreateVariationSettings_Normalizes_Above_Default_Through_Avar()
        {
            // wght=700 → linear normalization: (700-400) / (900-400) = 0.6
            //         → avar wght segment map: 0.6 → 0.54 (per the bend declared in
            //           Inter Variable's avar table, verified in AvarTableTests).
            var typeface = LoadTypeface(InterVariableAsset);

            var settings = typeface.CreateVariationSettings(
                new Dictionary<OpenTypeTag, float> { [s_wghtTag] = 700f });

            Assert.True(settings.TryGetCoordinate(s_wghtTag, out var wght));
            Assert.Equal(0.54f, wght, precision: 4);
        }

        [Fact]
        public void CreateVariationSettings_Normalizes_Below_Default()
        {
            // wght=200 → (200-400) / (400-100) = -2/3 ≈ -0.6667
            //         → avar wght map below 0 is linear identity through (-1, 0):
            //           (-1 → -1), (0 → 0); midpoint -2/3 stays -2/3.
            var typeface = LoadTypeface(InterVariableAsset);

            var settings = typeface.CreateVariationSettings(
                new Dictionary<OpenTypeTag, float> { [s_wghtTag] = 200f });

            Assert.True(settings.TryGetCoordinate(s_wghtTag, out var wght));
            Assert.Equal(-0.6667f, wght, precision: 3);
        }

        [Fact]
        public void CreateVariationSettings_Clamps_Above_Maximum()
        {
            // wght=10000 is silently clamped to the axis maximum (900) → +1 normalized
            //  → avar (1 → 1) = +1 corrected. Matches CSS and DirectWrite behavior.
            var typeface = LoadTypeface(InterVariableAsset);

            var settings = typeface.CreateVariationSettings(
                new Dictionary<OpenTypeTag, float> { [s_wghtTag] = 10000f });

            Assert.True(settings.TryGetCoordinate(s_wghtTag, out var wght));
            Assert.Equal(1f, wght, precision: 4);
        }

        [Fact]
        public void CreateVariationSettings_Clamps_Below_Minimum()
        {
            var typeface = LoadTypeface(InterVariableAsset);

            var settings = typeface.CreateVariationSettings(
                new Dictionary<OpenTypeTag, float> { [s_wghtTag] = -500f });

            Assert.True(settings.TryGetCoordinate(s_wghtTag, out var wght));
            Assert.Equal(-1f, wght, precision: 4);
        }

        [Fact]
        public void CreateVariationSettings_Ignores_Unknown_Axes()
        {
            // Axes not declared by the font are silently dropped — matches the
            // typeface-agnostic semantics documented on FontVariationSettings.
            var typeface = LoadTypeface(InterVariableAsset);

            var bogus = OpenTypeTag.Parse("xxxx");
            var settings = typeface.CreateVariationSettings(
                new Dictionary<OpenTypeTag, float>
                {
                    [bogus] = 0.5f,
                    [s_wghtTag] = 400f, // at default
                });

            Assert.True(settings.IsDefault);
            Assert.False(settings.TryGetCoordinate(bogus, out _));
        }

        [Fact]
        public void CreateVariationSettings_Uses_Named_Instance_Coordinates()
        {
            // Instance index 8 in Inter Variable is the last preset (Black, wght=900).
            // Picking it through the shorthand parameter should produce the same settings
            // as requesting wght=900 explicitly.
            var typeface = LoadTypeface(InterVariableAsset);
            var lastIndex = typeface.NamedInstances.Count - 1;

            var byIndex = typeface.CreateVariationSettings(null, instanceIndex: lastIndex);
            var byCoords = typeface.CreateVariationSettings(
                new Dictionary<OpenTypeTag, float> { [s_wghtTag] = 900f });

            Assert.Equal(byCoords, byIndex);
        }

        [Fact]
        public void CreateVariationSettings_Overrides_Named_Instance_With_Explicit_Coords()
        {
            // Instance index + explicit coords: the explicit coord wins per axis. Useful
            // for "start from Bold, but bump optical size".
            var typeface = LoadTypeface(InterVariableAsset);
            var lastIndex = typeface.NamedInstances.Count - 1;  // Black (wght=900)

            var settings = typeface.CreateVariationSettings(
                new Dictionary<OpenTypeTag, float> { [s_wghtTag] = 400f },
                instanceIndex: lastIndex);

            // wght was forced back to the default → no contribution to the settings,
            // and opsz stayed at the instance's default → also no contribution.
            Assert.True(settings.IsDefault);
        }

        [Fact]
        public void CreateVariationSettings_Throws_For_Invalid_Instance_Index()
        {
            var typeface = LoadTypeface(InterVariableAsset);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                typeface.CreateVariationSettings(null, instanceIndex: 99));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                typeface.CreateVariationSettings(null, instanceIndex: -1));
        }

        [Fact]
        public void CreateVariationSettings_Ignores_InstanceIndex_For_Static_Font()
        {
            // Static fonts short-circuit to default before validating the instance index,
            // matching the "static fonts are inert to variation" pattern. Surfaces in pr4b
            // when a global wght=600 is applied to a UI mixing static + variable fonts.
            var typeface = LoadTypeface(InterRegularAsset);

            Assert.True(typeface.CreateVariationSettings(null, instanceIndex: 99).IsDefault);
        }
    }
}
