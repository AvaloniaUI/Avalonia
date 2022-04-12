using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media
{
    public class SKTypefaceCollectionCacheTests
    {
        private const string s_notoMono =
            "resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#Noto Mono";
        
        [InlineData(s_notoMono, FontWeight.SemiLight, FontStyle.Normal)]
        [InlineData(s_notoMono, FontWeight.Bold, FontStyle.Italic)]
        [InlineData(s_notoMono, FontWeight.Heavy, FontStyle.Oblique)]
        [Theory]
        public void Should_Get_Near_Matching_Typeface(string familyName, FontWeight fontWeight, FontStyle fontStyle)
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var fontFamily = new FontFamily(familyName);
                
                var typefaceCollection = SKTypefaceCollectionCache.GetOrAddTypefaceCollection(fontFamily);

                var actual = typefaceCollection.Get(new Typeface(fontFamily, fontStyle, fontWeight))?.FamilyName;
                
                Assert.Equal("Noto Mono", actual);
            }
        }
        
        [Fact]
        public void Should_Get_Typeface_For_Invalid_FamilyName()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var notoMono =
                    new FontFamily("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#Noto Mono");
                
                var notoMonoCollection = SKTypefaceCollectionCache.GetOrAddTypefaceCollection(notoMono);

                var typeface = notoMonoCollection.Get(new Typeface("ABC"));
                
                Assert.NotNull(typeface);
            }
        }

        [Fact]
        public void Should_Get_Typeface_For_Partial_FamilyName()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var fontFamily = new FontFamily("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#T");

                var fontCollection = SKTypefaceCollectionCache.GetOrAddTypefaceCollection(fontFamily);

                var typeface = fontCollection.Get(new Typeface(fontFamily));

                Assert.NotNull(typeface);

                Assert.Equal("Twitter Color Emoji", typeface.FamilyName);
            }
        }
    }
}
