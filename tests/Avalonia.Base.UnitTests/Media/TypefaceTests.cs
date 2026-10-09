using System;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class TypefaceTests
    {
        [Fact]
        public void Exception_Should_Be_Thrown_If_FontWeight_LessThanEqualTo_Zero()
        {
            Assert.Throws<ArgumentException>(() => new Typeface("foo", (FontStyle)12, 0));
        }

        [Fact]
        public void Should_Be_Equal()
        {
            Assert.Equal(new Typeface("Font A"), new Typeface("Font A"));
        }

        [Fact]
        public void Should_Have_Equal_Hash()
        {
            Assert.Equal(new Typeface("Font A").GetHashCode(), new Typeface("Font A").GetHashCode());
        }

        [Fact]
        public void Typefaces_With_Equal_FontVariations_Are_Equal_With_Equal_Hash()
        {
            var a = new Typeface("Font A", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, FontVariationSettings.Parse("wght=700"));
            var b = new Typeface("Font A", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, FontVariationSettings.Parse("wght=700"));

            Assert.Equal(a, b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void Typefaces_With_Different_FontVariations_Are_Not_Equal()
        {
            var plain = new Typeface("Font A");
            var bold = new Typeface("Font A", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, FontVariationSettings.Parse("wght=700"));
            var black = new Typeface("Font A", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, FontVariationSettings.Parse("wght=900"));

            Assert.NotEqual(plain, bold);
            Assert.NotEqual(bold, black);
        }

        [Fact]
        public void Empty_FontVariations_Normalize_To_Null()
        {
            // null and Empty both mean "design defaults"; the ctor stores the canonical
            // form so equality, hashing and cache keys never split the two spellings.
            var withNull = new Typeface("Font A", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, null);
            var withEmpty = new Typeface("Font A", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, FontVariationSettings.Empty);

            Assert.Null(withEmpty.FontVariations);
            Assert.Equal(withNull, withEmpty);
            Assert.Equal(withNull.GetHashCode(), withEmpty.GetHashCode());
        }

        [Fact]
        public void Normalize_Preserves_FontVariations()
        {
            var typeface = new Typeface("Hello World Italic",
                FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, FontVariationSettings.Parse("wght=650"));

            var normalized = typeface.Normalize(out _);

            Assert.Equal(FontStyle.Italic, normalized.Style);
            Assert.Equal(typeface.FontVariations, normalized.FontVariations);
        }

        [InlineData("Hello World 6", "Hello World 6", FontStyle.Normal, FontWeight.Normal)]
        [InlineData("Hello World Italic", "Hello World", FontStyle.Italic, FontWeight.Normal)]
        [InlineData("Hello World Italic Bold", "Hello World", FontStyle.Italic, FontWeight.Bold)]
        [InlineData("FontAwesome 6 Free Regular", "FontAwesome 6 Free", FontStyle.Normal, FontWeight.Normal)]
        [InlineData("FontAwesome 6 Free Solid", "FontAwesome 6 Free", FontStyle.Normal, FontWeight.Solid)]
        [InlineData("FontAwesome 6 Brands", "FontAwesome 6 Brands", FontStyle.Normal, FontWeight.Normal)]
        [Theory]
        public void Should_Get_Implicit_Typeface(string input, string familyName, FontStyle style, FontWeight weight)
        {
            var typeface = new Typeface(input);

            var normalizedTypeface = typeface.Normalize(out var normalizedFamilyName);

            Assert.Equal(familyName, normalizedFamilyName);
            Assert.Equal(style, normalizedTypeface.Style);
            Assert.Equal(weight, normalizedTypeface.Weight);
            Assert.Equal(FontStretch.Normal, normalizedTypeface.Stretch);
        }
    }
}
