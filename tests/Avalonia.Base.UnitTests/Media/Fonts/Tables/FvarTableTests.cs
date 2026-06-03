using System;
using System.IO;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Media.Fonts.Tables.Name;
using Avalonia.Media.Fonts.Tables.Variation;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts.Tables
{
    public class FvarTableTests
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
            var nameTable = NameTable.Load(typeface);

            Assert.False(FvarTable.TryLoad(typeface, nameTable, out var fvar));
            Assert.Null(fvar);
        }

        [Fact]
        public void TryLoad_Returns_True_For_Inter_Variable()
        {
            var typeface = LoadTypeface(InterVariableAsset);
            var nameTable = NameTable.Load(typeface);

            Assert.True(FvarTable.TryLoad(typeface, nameTable, out var fvar));
            Assert.NotNull(fvar);
        }

        [Fact]
        public void Axes_Reports_Inter_Variable_Axes()
        {
            // Inter Variable carries two axes: opsz (14..32, default 14) and wght (100..900,
            // default 400). Verified directly against the font's binary fvar table.
            var typeface = LoadTypeface(InterVariableAsset);
            var nameTable = NameTable.Load(typeface);
            Assert.True(FvarTable.TryLoad(typeface, nameTable, out var fvar));

            Assert.Equal(2, fvar!.Axes.Length);

            var opsz = fvar.Axes[0];
            Assert.Equal(OpenTypeTag.Parse("opsz"), opsz.Tag);
            Assert.Equal(14f, opsz.MinimumValue);
            Assert.Equal(14f, opsz.DefaultValue);
            Assert.Equal(32f, opsz.MaximumValue);
            Assert.False(opsz.IsHidden);

            var wght = fvar.Axes[1];
            Assert.Equal(OpenTypeTag.Parse("wght"), wght.Tag);
            Assert.Equal(100f, wght.MinimumValue);
            Assert.Equal(400f, wght.DefaultValue);
            Assert.Equal(900f, wght.MaximumValue);
            Assert.False(wght.IsHidden);
        }

        [Fact]
        public void Axis_Names_Resolve_When_Name_Table_Present()
        {
            // The fvar axisNameID points into the name table. Resolution should produce a
            // non-tag name when the table is available — Inter Variable's name records use
            // "Optical Size" and "Weight". We only assert the names are non-empty and
            // distinct from the raw tag, to avoid brittleness if Inter renames them.
            var typeface = LoadTypeface(InterVariableAsset);
            var nameTable = NameTable.Load(typeface);
            Assert.True(FvarTable.TryLoad(typeface, nameTable, out var fvar));

            foreach (var axis in fvar!.Axes)
            {
                Assert.False(string.IsNullOrWhiteSpace(axis.Name));
                Assert.NotEqual(axis.Tag.ToString(), axis.Name);
            }
        }

        [Fact]
        public void Axis_Names_Fall_Back_To_Tag_When_Name_Table_Missing()
        {
            var typeface = LoadTypeface(InterVariableAsset);
            Assert.True(FvarTable.TryLoad(typeface, nameTable: null, out var fvar));

            foreach (var axis in fvar!.Axes)
            {
                Assert.Equal(axis.Tag.ToString(), axis.Name);
            }
        }

        [Fact]
        public void Instances_Reports_All_Nine_Inter_Variable_Presets()
        {
            // Inter Variable declares 9 named instances (Thin through Black, all at the
            // default opsz=14). The first three are wght=100/200/300 — verified directly
            // against the binary fvar.
            var typeface = LoadTypeface(InterVariableAsset);
            var nameTable = NameTable.Load(typeface);
            Assert.True(FvarTable.TryLoad(typeface, nameTable, out var fvar));

            Assert.Equal(9, fvar!.Instances.Length);

            // The default-instance preset is at index 3 — wght=400 in Inter Variable.
            var defaultInstance = fvar.Instances[3];
            Assert.Equal(3, defaultInstance.Index);
            Assert.Equal(400f, defaultInstance.Coordinates[OpenTypeTag.Parse("wght")]);
            Assert.Equal(14f, defaultInstance.Coordinates[OpenTypeTag.Parse("opsz")]);
        }

        [Fact]
        public void Instance_Coordinates_Match_fvar_Data()
        {
            // First instance is Thin at wght=100; last is Black at wght=900. Both at
            // opsz=14 since Inter Variable's named instances are all at the default opsz.
            var typeface = LoadTypeface(InterVariableAsset);
            var nameTable = NameTable.Load(typeface);
            Assert.True(FvarTable.TryLoad(typeface, nameTable, out var fvar));

            var thin = fvar!.Instances[0];
            Assert.Equal(100f, thin.Coordinates[OpenTypeTag.Parse("wght")]);

            var black = fvar.Instances[fvar.Instances.Length - 1];
            Assert.Equal(900f, black.Coordinates[OpenTypeTag.Parse("wght")]);
        }

        [Fact]
        public void Instances_Have_Sequential_Indices()
        {
            // Index uniquely identifies an instance within a font — used as the lookup key
            // for CreateVariationSettings(instanceIndex: ...).
            var typeface = LoadTypeface(InterVariableAsset);
            var nameTable = NameTable.Load(typeface);
            Assert.True(FvarTable.TryLoad(typeface, nameTable, out var fvar));

            for (var i = 0; i < fvar!.Instances.Length; i++)
            {
                Assert.Equal(i, fvar.Instances[i].Index);
            }
        }

        [Fact]
        public void AxisTags_Are_Available_For_Indexed_Lookup()
        {
            // AxisTags exists so downstream consumers (avar, gvar, HVAR) can index by axis
            // position without unpacking the full FontVariationAxis record. The order must
            // match the public Axes list.
            var typeface = LoadTypeface(InterVariableAsset);
            var nameTable = NameTable.Load(typeface);
            Assert.True(FvarTable.TryLoad(typeface, nameTable, out var fvar));

            Assert.Equal(fvar!.Axes.Length, fvar.AxisTags.Length);
            for (var i = 0; i < fvar.Axes.Length; i++)
            {
                Assert.Equal(fvar.Axes[i].Tag, fvar.AxisTags[i]);
            }
        }
    }
}
