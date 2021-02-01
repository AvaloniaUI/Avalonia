using System;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media
{
    public class GlyphRunTests
    {
        [InlineData("ABC \r", 29, 4, 1)]
        [InlineData("ABC \r", 23, 3, 1)]
        [InlineData("ABC \r", 17, 2, 1)]
        [InlineData("ABC \r", 11, 1, 1)]
        [InlineData("ABC \r", 7, 1, 0)]
        [InlineData("ABC \r", 5, 0, 1)]
        [InlineData("ABC \r", 2, 0, 0)]
        [Theory]
        public void Should_Get_Distance_From_CharacterHit(string text, double distance, int expectedIndex,
            int expectedTrailingLength)
        {
            using (Start())
            {
                var glyphRun =
                    TextShaper.Current.ShapeText(text.AsMemory(), Typeface.Default, 10, CultureInfo.CurrentCulture);

                var characterHit = glyphRun.GetCharacterHitFromDistance(distance, out _);
                
                Assert.Equal(expectedIndex, characterHit.FirstCharacterIndex);
                
                Assert.Equal(expectedTrailingLength, characterHit.TrailingLength);
            }
        }
        
        private static IDisposable Start()
        {
            var disposable = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
                .With(renderInterface: new PlatformRenderInterface(null),
                    textShaperImpl: new TextShaperImpl(),
                    fontManagerImpl: new CustomFontManagerImpl()));

            return disposable;
        }
    }
}
