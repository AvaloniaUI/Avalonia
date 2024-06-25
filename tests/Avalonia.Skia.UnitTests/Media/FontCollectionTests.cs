using Avalonia.Media;
using Avalonia.Media.Fonts;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media
{
    public class FontCollectionTests
    {
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

            var result = FontCollectionBase.GetImplicitTypeface(typeface, out var normalizedFamilyName);

            Assert.Equal(familyName, normalizedFamilyName);
            Assert.Equal(style, result.Style);
            Assert.Equal(weight, result.Weight);
            Assert.Equal(FontStretch.Normal, result.Stretch);
        }
    }
}
