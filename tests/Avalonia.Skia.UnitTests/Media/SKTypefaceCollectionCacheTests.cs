using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media
{
    public class SKTypefaceCollectionCacheTests
    {
        [Fact]
        public void Should_Get_Near_Matching_Typeface()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var notoMono =
                    new FontFamily("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#Noto Mono");

                var notoMonoCollection = SKTypefaceCollectionCache.GetOrAddTypefaceCollection(notoMono);

                Assert.Equal("Noto Mono",
                    notoMonoCollection.Get(new Typeface(notoMono, weight: FontWeight.Bold)).FamilyName);
            }
        }
        
        [Fact]
        public void Should_Get_Null_For_Invalid_FamilyName()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var notoMono =
                    new FontFamily("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#Noto Mono");
                
                var notoMonoCollection = SKTypefaceCollectionCache.GetOrAddTypefaceCollection(notoMono);

                var typeface = notoMonoCollection.Get(new Typeface("ABC"));
                
                Assert.Null(typeface);
            }
        }
    }
}
