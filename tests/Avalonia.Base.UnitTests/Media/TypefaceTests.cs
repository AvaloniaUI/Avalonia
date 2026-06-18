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
