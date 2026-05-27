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
        public void Default_Struct_Is_The_No_Variation_Case()
        {
            var settings = default(FontVariationSettings);

            Assert.True(settings.IsDefault);
            Assert.Empty(settings.Coordinates);
            Assert.Equal(0, settings.GetHashCode());
        }

        [Fact]
        public void Default_Structs_Are_Equal()
        {
            // Two zero-initialized structs must compare equal, even though they're
            // distinct values on the stack. This is the substitute for the previous
            // singleton Default.
            Assert.Equal(default(FontVariationSettings), default(FontVariationSettings));
            Assert.True(default(FontVariationSettings) == default(FontVariationSettings));
        }

        [Fact]
        public void Coordinates_Property_Returns_Empty_Not_Default_For_Default_Struct()
        {
            // The IsDefault → Empty normalization in the property lets callers iterate
            // / index without first checking ImmutableArray.IsDefault.
            var settings = default(FontVariationSettings);

            Assert.False(settings.Coordinates.IsDefault);
            Assert.Equal(0, settings.Coordinates.Length);
        }

        [Fact]
        public void FromCoordinates_Dictionary_Throws_On_Null()
        {
            Assert.Throws<ArgumentNullException>(
                () => FontVariationSettings.FromCoordinates((IReadOnlyDictionary<OpenTypeTag, float>)null!));
        }

        [Theory]
        [InlineData(float.NaN)]
        [InlineData(-1.0001f)]
        [InlineData(1.0001f)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity)]
        public void FromCoordinates_Dictionary_Rejects_Out_Of_Range_Or_NaN(float value)
        {
            var coords = new Dictionary<OpenTypeTag, float> { [Wght] = value };

            Assert.Throws<ArgumentOutOfRangeException>(
                () => FontVariationSettings.FromCoordinates(coords));
        }

        [Theory]
        [InlineData(-1f)]
        [InlineData(0f)]
        [InlineData(1f)]
        public void FromCoordinates_Dictionary_Accepts_Boundary_Values(float value)
        {
            var coords = new Dictionary<OpenTypeTag, float> { [Wght] = value };

            var settings = FontVariationSettings.FromCoordinates(coords);

            Assert.Single(settings.Coordinates);
            Assert.Equal(Wght, settings.Coordinates[0].Axis);
            Assert.Equal(value, settings.Coordinates[0].NormalizedValue);
        }

        [Fact]
        public void FromCoordinates_Dictionary_Empty_Returns_Default_Struct()
        {
            var settings = FontVariationSettings.FromCoordinates(new Dictionary<OpenTypeTag, float>());

            Assert.True(settings.IsDefault);
            Assert.Equal(default(FontVariationSettings), settings);
        }

        [Fact]
        public void FromCoordinates_Dictionary_Sorts_By_Axis_Tag()
        {
            // Insertion order shouldn't matter — coordinates land sorted by (uint)tag
            // so equality and hashing are insertion-order-independent.
            var unordered = new Dictionary<OpenTypeTag, float>
            {
                [Wght] = 0.5f,
                [Ital] = 1f,
                [Wdth] = -0.25f,
            };

            var settings = FontVariationSettings.FromCoordinates(unordered);

            Assert.Equal(3, settings.Coordinates.Length);
            // Sorted: ital (0x6974616c), wdth (0x77647468), wght (0x77676874).
            // Lexically by the 4-char ASCII tag uint, that's ital < wdth < wght.
            Assert.Equal(Ital, settings.Coordinates[0].Axis);
            Assert.Equal(Wdth, settings.Coordinates[1].Axis);
            Assert.Equal(Wght, settings.Coordinates[2].Axis);
        }

        [Fact]
        public void FromCoordinates_Dictionary_Defensively_Copies_The_Input()
        {
            var mutable = new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f };

            var settings = FontVariationSettings.FromCoordinates(mutable);

            mutable[Wght] = 0.9f;
            mutable[Wdth] = -0.25f;

            Assert.Single(settings.Coordinates);
            Assert.Equal(0.5f, settings.Coordinates[0].NormalizedValue);
        }

        [Fact]
        public void FromCoordinates_Span_Empty_Returns_Default_Struct()
        {
            var settings = FontVariationSettings.FromCoordinates(ReadOnlySpan<FontVariationCoordinate>.Empty);

            Assert.True(settings.IsDefault);
        }

        [Fact]
        public void FromCoordinates_Span_Sorts_And_Validates()
        {
            Span<FontVariationCoordinate> coords =
            [
                new FontVariationCoordinate(Wght, 0.5f),
                new FontVariationCoordinate(Ital, 1f),
                new FontVariationCoordinate(Wdth, -0.25f),
            ];

            var settings = FontVariationSettings.FromCoordinates(coords);

            Assert.Equal(3, settings.Coordinates.Length);
            Assert.Equal(Ital, settings.Coordinates[0].Axis);
            Assert.Equal(Wdth, settings.Coordinates[1].Axis);
            Assert.Equal(Wght, settings.Coordinates[2].Axis);
        }

        [Fact]
        public void FromCoordinates_Span_Rejects_Duplicate_Axes()
        {
            Assert.Throws<ArgumentException>(static () =>
            {
                Span<FontVariationCoordinate> coords =
                [
                    new FontVariationCoordinate(OpenTypeTag.Parse("wght"), 0.5f),
                    new FontVariationCoordinate(OpenTypeTag.Parse("wght"), -0.5f),
                ];
                FontVariationSettings.FromCoordinates(coords);
            });
        }

        [Fact]
        public void FromCoordinates_Span_Rejects_Out_Of_Range_Value()
        {
            Assert.Throws<ArgumentOutOfRangeException>(static () =>
            {
                Span<FontVariationCoordinate> coords = [new FontVariationCoordinate(OpenTypeTag.Parse("wght"), 2f)];
                FontVariationSettings.FromCoordinates(coords);
            });
        }

        [Fact]
        public void TryGetCoordinate_Returns_True_For_Present_Axis()
        {
            var settings = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f, [Wdth] = -0.25f });

            Assert.True(settings.TryGetCoordinate(Wght, out var w));
            Assert.Equal(0.5f, w);

            Assert.True(settings.TryGetCoordinate(Wdth, out var wd));
            Assert.Equal(-0.25f, wd);
        }

        [Fact]
        public void TryGetCoordinate_Returns_False_For_Absent_Axis()
        {
            var settings = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f });

            Assert.False(settings.TryGetCoordinate(Ital, out var v));
            Assert.Equal(0f, v);
        }

        [Fact]
        public void TryGetCoordinate_Returns_False_For_Default_Struct()
        {
            var settings = default(FontVariationSettings);

            Assert.False(settings.TryGetCoordinate(Wght, out var v));
            Assert.Equal(0f, v);
        }

        [Fact]
        public void GetCoordinateOrDefault_Returns_Fallback_For_Absent_Axis()
        {
            var settings = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f });

            Assert.Equal(0.5f, settings.GetCoordinateOrDefault(Wght));
            Assert.Equal(0f, settings.GetCoordinateOrDefault(Ital));
            Assert.Equal(-1f, settings.GetCoordinateOrDefault(Ital, -1f));
        }

        [Fact]
        public void Equality_Is_Reflexive()
        {
            var settings = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f });

            Assert.True(settings.Equals(settings));
        }

        [Fact]
        public void Equality_Is_Structural_For_Identical_Coordinates()
        {
            var a = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f, [Wdth] = -0.25f });
            var b = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f, [Wdth] = -0.25f });

            Assert.True(a.Equals(b));
            Assert.True(b.Equals(a));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void Equality_Ignores_Insertion_Order()
        {
            var a = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f, [Wdth] = -0.25f });
            var b = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Wdth] = -0.25f, [Wght] = 0.5f });

            Assert.True(a.Equals(b));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void Equality_Differs_When_A_Coordinate_Value_Differs()
        {
            var a = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f });
            var b = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Wght] = 0.6f });

            Assert.False(a.Equals(b));
        }

        [Fact]
        public void Equality_Differs_When_A_Coordinate_Key_Differs()
        {
            var a = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f });
            var b = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Wdth] = 0.5f });

            Assert.False(a.Equals(b));
        }

        [Fact]
        public void Equality_Differs_When_Coordinate_Counts_Differ()
        {
            var a = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f });
            var b = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f, [Wdth] = 0f });

            Assert.False(a.Equals(b));
        }

        [Fact]
        public void Equality_Differs_Between_Default_And_Populated()
        {
            var a = default(FontVariationSettings);
            var b = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f });

            Assert.False(a.Equals(b));
            Assert.False(b.Equals(a));
        }

        [Fact]
        public void Hash_Is_Cached_And_Stable_Across_Equal_Instances()
        {
            // The hash is computed once at construction. Two structurally-equal settings
            // must have the same hash regardless of how the coordinates were inserted.
            var a = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f, [Wdth] = -0.25f, [Ital] = 1f });
            var b = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Ital] = 1f, [Wght] = 0.5f, [Wdth] = -0.25f });

            var hashA = a.GetHashCode();
            // Subsequent calls return the same cached value.
            Assert.Equal(hashA, a.GetHashCode());
            Assert.Equal(hashA, b.GetHashCode());
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
        }

        [Fact]
        public void Equality_With_Boxed_Object()
        {
            // Cache-key paths box rarely, but Equals(object) must still be correct.
            var a = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f });
            object boxed = FontVariationSettings.FromCoordinates(
                new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f });

            Assert.True(a.Equals(boxed));
            Assert.False(a.Equals((object?)null));
            Assert.False(a.Equals("not a settings"));
        }

        [Fact]
        public void FontVariationCoordinate_Has_Structural_Equality()
        {
            // The coordinate record-struct provides equality for free; verify it
            // behaves as expected so callers can use it in their own comparisons.
            var a = new FontVariationCoordinate(Wght, 0.5f);
            var b = new FontVariationCoordinate(Wght, 0.5f);
            var c = new FontVariationCoordinate(Wght, 0.6f);
            var d = new FontVariationCoordinate(Wdth, 0.5f);

            Assert.Equal(a, b);
            Assert.NotEqual(a, c);
            Assert.NotEqual(a, d);
            Assert.True(a == b);
            Assert.True(a != c);
        }
    }
}
