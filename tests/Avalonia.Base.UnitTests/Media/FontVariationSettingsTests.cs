using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class FontVariationSettingsTests
    {
        private static readonly OpenTypeTag Wght = OpenTypeTag.Parse("wght");
        private static readonly OpenTypeTag Wdth = OpenTypeTag.Parse("wdth");
        private static readonly OpenTypeTag Ital = OpenTypeTag.Parse("ital");

        [Fact]
        public void Default_Has_Empty_Coordinates_And_Null_InstanceIndex()
        {
            var settings = FontVariationSettings.Default;

            Assert.NotNull(settings);
            Assert.Empty(settings.NormalizedCoordinates);
            Assert.Null(settings.InstanceIndex);
        }

        [Fact]
        public void Default_Is_A_Singleton()
        {
            Assert.Same(FontVariationSettings.Default, FontVariationSettings.Default);
        }

        [Fact]
        public void FromCoordinates_Throws_On_Null_Map()
        {
            Assert.Throws<ArgumentNullException>(
                () => FontVariationSettings.FromCoordinates(null!));
        }

        [Theory]
        [InlineData(float.NaN)]
        [InlineData(-1.0001f)]
        [InlineData(1.0001f)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity)]
        public void FromCoordinates_Rejects_Out_Of_Range_Or_NaN(float value)
        {
            var coords = new Dictionary<OpenTypeTag, float> { [Wght] = value };

            Assert.Throws<ArgumentOutOfRangeException>(
                () => FontVariationSettings.FromCoordinates(coords));
        }

        [Theory]
        [InlineData(-1f)]
        [InlineData(0f)]
        [InlineData(1f)]
        public void FromCoordinates_Accepts_Boundary_Values(float value)
        {
            var coords = new Dictionary<OpenTypeTag, float> { [Wght] = value };

            var settings = FontVariationSettings.FromCoordinates(coords);

            Assert.Single(settings.NormalizedCoordinates);
            Assert.Equal(value, settings.NormalizedCoordinates[Wght]);
        }

        [Fact]
        public void FromCoordinates_Rejects_Negative_InstanceIndex()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => FontVariationSettings.FromCoordinates(
                    new Dictionary<OpenTypeTag, float>(), instanceIndex: -1));
        }

        [Fact]
        public void FromCoordinates_With_Empty_Map_And_No_Instance_Returns_Default()
        {
            // When the inputs carry no actual configuration, the factory returns the
            // shared Default singleton instead of allocating a fresh instance.
            var settings = FontVariationSettings.FromCoordinates(new Dictionary<OpenTypeTag, float>());

            Assert.Same(FontVariationSettings.Default, settings);
        }

        [Fact]
        public void FromCoordinates_Stores_InstanceIndex()
        {
            var settings = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f },
                instanceIndex: 3);

            Assert.Equal(3, settings.InstanceIndex);
        }

        [Fact]
        public void FromCoordinates_Defensively_Copies_The_Input_Dictionary()
        {
            var mutable = new Dictionary<OpenTypeTag, float>
            {
                [Wght] = 0.5f,
            };

            var settings = FontVariationSettings.FromCoordinates(mutable);

            // Mutation of the caller's dictionary after construction must not leak
            // into the settings instance.
            mutable[Wght] = 0.9f;
            mutable[Wdth] = -0.25f;

            Assert.Single(settings.NormalizedCoordinates);
            Assert.Equal(0.5f, settings.NormalizedCoordinates[Wght]);
            Assert.False(settings.NormalizedCoordinates.ContainsKey(Wdth));
        }

        [Fact]
        public void NormalizedCoordinates_Is_Read_Only()
        {
            var settings = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f });

            // FrozenDictionary implements IDictionary<K,V> but is read-only:
            // IsReadOnly returns true and any mutation attempt throws.
            var mutableView = settings.NormalizedCoordinates as IDictionary<OpenTypeTag, float>;
            Assert.NotNull(mutableView);
            Assert.True(mutableView!.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => mutableView[Wght] = 0.9f);
        }

        [Fact]
        public void FromInstance_Rejects_Negative_Index()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => FontVariationSettings.FromInstance(-1));
        }

        [Fact]
        public void FromInstance_Returns_Settings_With_Empty_Coordinates_And_Given_Index()
        {
            var settings = FontVariationSettings.FromInstance(2);

            Assert.Empty(settings.NormalizedCoordinates);
            Assert.Equal(2, settings.InstanceIndex);
        }

        [Fact]
        public void Equals_Is_Reflexive()
        {
            var settings = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f });

            Assert.True(settings.Equals(settings));
        }

        [Fact]
        public void Equals_Is_Structural_For_Identical_Coordinates()
        {
            var a = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f, [Wdth] = -0.25f });
            var b = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f, [Wdth] = -0.25f });

            Assert.NotSame(a, b);
            Assert.True(a.Equals(b));
            Assert.True(b.Equals(a));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void Equals_Ignores_Insertion_Order_Of_Coordinates()
        {
            var a = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f, [Wdth] = -0.25f });
            var b = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Wdth] = -0.25f, [Wght] = 0.5f });

            Assert.True(a.Equals(b));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void Equals_Differs_When_A_Coordinate_Value_Differs()
        {
            var a = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f });
            var b = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Wght] = 0.6f });

            Assert.False(a.Equals(b));
        }

        [Fact]
        public void Equals_Differs_When_A_Coordinate_Key_Differs()
        {
            var a = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f });
            var b = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Wdth] = 0.5f });

            Assert.False(a.Equals(b));
        }

        [Fact]
        public void Equals_Differs_When_Coordinate_Counts_Differ()
        {
            var a = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f });
            var b = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f, [Wdth] = 0f });

            Assert.False(a.Equals(b));
        }

        [Fact]
        public void Equals_Differs_When_InstanceIndex_Differs()
        {
            var a = FontVariationSettings.FromInstance(0);
            var b = FontVariationSettings.FromInstance(1);

            Assert.False(a.Equals(b));
        }

        [Fact]
        public void Equals_Differs_Between_Instance_And_Coordinate_Forms()
        {
            var a = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f });
            var b = FontVariationSettings.FromInstance(0);

            Assert.False(a.Equals(b));
        }

        [Fact]
        public void Equals_Returns_False_For_Null()
        {
            var settings = FontVariationSettings.FromInstance(0);

            Assert.False(settings.Equals(null));
            Assert.False(settings.Equals((object?)null));
        }

        [Fact]
        public void Equality_Operators_Match_Equals()
        {
            var a = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Ital] = 1f });
            var b = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Ital] = 1f });
            var c = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Ital] = 0f });

            Assert.True(a == b);
            Assert.False(a != b);
            Assert.False(a == c);
            Assert.True(a != c);

            FontVariationSettings? nullSettings = null;
            Assert.True(nullSettings == null);
            Assert.False(a == null);
            Assert.False(null == a);
        }
    }
}
