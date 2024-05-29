using System;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media
{
    public class EmbeddedFontCollectionTests
    {
        private const string s_notoMono =
            "resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#Noto Mono";
        
        [InlineData(FontWeight.SemiLight, FontStyle.Normal)]
        [InlineData(FontWeight.Bold, FontStyle.Italic)]
        [InlineData(FontWeight.Heavy, FontStyle.Oblique)]
        [Theory]
        public void Should_Get_Near_Matching_Typeface(FontWeight fontWeight, FontStyle fontStyle)
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var source = new Uri(s_notoMono, UriKind.Absolute);

                var fontCollection = new EmbeddedFontCollection(source, source);

                fontCollection.Initialize(new CustomFontManagerImpl());

                Assert.True(fontCollection.TryGetGlyphTypeface("Noto Mono", fontStyle, fontWeight, FontStretch.Normal, out var glyphTypeface));

                var actual = glyphTypeface?.FamilyName;

                Assert.Equal("Noto Mono", actual);
            }
        }
        
        [Fact]
        public void Should_Not_Get_Typeface_For_Invalid_FamilyName()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var source = new Uri(s_notoMono, UriKind.Absolute);

                var fontCollection = new EmbeddedFontCollection(source, source);

                fontCollection.Initialize(new CustomFontManagerImpl());

                Assert.False(fontCollection.TryGetGlyphTypeface("ABC", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, out var glyphTypeface));
            }
        }

        [Fact]
        public void Should_Get_Typeface_For_Partial_FamilyName()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var source = new Uri("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#T", UriKind.Absolute);

                var fontCollection = new EmbeddedFontCollection(source, source);

                fontCollection.Initialize(new CustomFontManagerImpl());

                Assert.True(fontCollection.TryGetGlyphTypeface("T", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, out var glyphTypeface));

                Assert.Equal("Twitter Color Emoji", glyphTypeface.FamilyName);
            }
        }
    }
}
