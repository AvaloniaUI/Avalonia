using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media
{
    public class SKTypefaceCollectionCacheTests
    {
        [Fact]
        public void Should_Load_Typefaces_From_Invalid_Name()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var notoMono =
                    new FontFamily("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#Noto Mono");

                var colorEmoji =
                    new FontFamily("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#Twitter Color Emoji");

                var notoMonoCollection = SKTypefaceCollectionCache.GetOrAddTypefaceCollection(notoMono);

                var typeface = new Typeface("ABC", FontStyle.Italic, FontWeight.Bold);

                Assert.Equal("Noto Mono", notoMonoCollection.Get(typeface).FamilyName);

                var notoColorEmojiCollection = SKTypefaceCollectionCache.GetOrAddTypefaceCollection(colorEmoji);

                Assert.Equal("Twitter Color Emoji", notoColorEmojiCollection.Get(typeface).FamilyName);
            }
        }
    }
}
