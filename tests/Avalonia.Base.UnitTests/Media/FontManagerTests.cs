using System;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class FontManagerTests
    {
        [Fact]
        public void Should_Create_Single_Instance_Typeface()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var fontFamily = new FontFamily("MyFont");

                var typeface = new Typeface(fontFamily);

                var glyphTypeface = FontManager.Current.GetOrAddGlyphTypeface(typeface);

                Assert.Same(glyphTypeface, FontManager.Current.GetOrAddGlyphTypeface(typeface));
            }
        }

        [Fact]
        public void Should_Throw_When_Default_FamilyName_Is_Null()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new MockFontManagerImpl(null))))
            {
                Assert.Throws<InvalidOperationException>(() => FontManager.Current);
            }
        }

        [Fact]
        public void Should_Use_FontManagerOptions_DefaultFamilyName()
        {
            var options = new FontManagerOptions { DefaultFamilyName = "MyFont" };

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
                .With(fontManagerImpl: new MockFontManagerImpl())))
            {
                AvaloniaLocator.CurrentMutable.Bind<FontManagerOptions>().ToConstant(options);

                Assert.Equal("MyFont", FontManager.Current.DefaultFontFamilyName);
            }
        }

        [Fact]
        public void Should_Use_FontManagerOptions_FontFallback()
        {
            var options = new FontManagerOptions
            {
                FontFallbacks = new[]
                {
                    new FontFallback
                    {
                        FontFamily = new FontFamily("MyFont"), UnicodeRange = UnicodeRange.Default
                    }
                }
            };

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
                .With(fontManagerImpl: new MockFontManagerImpl())))
            {
                AvaloniaLocator.CurrentMutable.Bind<FontManagerOptions>().ToConstant(options);

                FontManager.Current.TryMatchCharacter(1, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal,
                    FontFamily.Default, null, out var typeface);

                Assert.Equal("MyFont", typeface.FontFamily.Name);
            }
        }
    }
}
