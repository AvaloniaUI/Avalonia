using System;
using System.Linq;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.UnitTests;
using SkiaSharp;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media
{
    public class FontManagerTests
    {
        private static string s_fontUri = "resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#Noto Mono";

        [Fact]
        public void Should_Create_Typeface_From_Fallback()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                var fontManager = FontManager.Current;

                var glyphTypeface = new Typeface(new FontFamily("A, B, " + FontFamily.DefaultFontFamilyName)).GlyphTypeface;

                Assert.Equal(SKTypeface.Default.FamilyName, glyphTypeface.FamilyName);
            }
        }

        [Fact]
        public void Should_Create_Typeface_From_Fallback_Bold()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                var glyphTypeface = new Typeface(new FontFamily($"A, B, Arial"), weight: FontWeight.Bold).GlyphTypeface;

                Assert.True((int)glyphTypeface.Weight >= 600);
            }
        }

        [Fact]
        public void Should_Yield_Default_GlyphTypeface_For_Invalid_FamilyName()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                var glyphTypeface = new Typeface(new FontFamily("Unknown")).GlyphTypeface;

                Assert.Equal(FontManager.Current.DefaultFontFamily.Name, glyphTypeface.FamilyName);
            }
        }

        [Fact]
        public void Should_Load_Typeface_From_Resource()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                var glyphTypeface = new Typeface(s_fontUri).GlyphTypeface;

                Assert.Equal("Noto Mono", glyphTypeface.FamilyName);
            }
        }

        [Fact]
        public void Should_Load_Nearest_Matching_Font()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                var glyphTypeface = new Typeface(s_fontUri, FontStyle.Italic, FontWeight.Black).GlyphTypeface;

                Assert.Equal("Noto Mono", glyphTypeface.FamilyName);
            }
        }

        [Fact]
        public void Should_Throw_For_Invalid_Custom_Font()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                Assert.Throws<InvalidOperationException>(() => new Typeface("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#Unknown").GlyphTypeface);
            }
        }

        [Fact]
        public void Should_Return_False_For_Unregistered_FontCollection_Uri()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                var result = FontManager.Current.TryGetGlyphTypeface(new Typeface("fonts:invalid#Something"), out _);

                Assert.False(result);
            }
        }

        [Fact]
        public void Should_Only_Try_To_Create_GlyphTypeface_Once()
        {
            var fontManagerImpl = new HeadlessFontManagerStub();

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManagerImpl)))
            {
                Assert.True(FontManager.Current.TryGetGlyphTypeface(Typeface.Default, out _));

                for (int i = 0;i < 10; i++)
                {
                    FontManager.Current.TryGetGlyphTypeface(new Typeface("Unknown"), out _);
                }

                Assert.Equal(fontManagerImpl.TryCreateGlyphTypefaceCount, 2);
            }
        }

        [Fact]
        public void Should_Load_Embedded_DefaultFontFamily()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                using (AvaloniaLocator.EnterScope())
                {
                    AvaloniaLocator.CurrentMutable.BindToSelf(new FontManagerOptions { DefaultFamilyName = s_fontUri });

                    var result = FontManager.Current.TryGetGlyphTypeface(Typeface.Default, out var glyphTypeface);

                    Assert.True(result);

                    Assert.Equal("Noto Mono", glyphTypeface.FamilyName);
                }
            }
        }

        [Fact]
        public void Should_Return_False_For_Invalid_DefaultFontFamily()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                using (AvaloniaLocator.EnterScope())
                {
                    AvaloniaLocator.CurrentMutable.BindToSelf(new FontManagerOptions { DefaultFamilyName = "avares://resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#Unknown" });

                    var result = FontManager.Current.TryGetGlyphTypeface(Typeface.Default, out _);

                    Assert.False(result);
                }
            }
        }

        [Fact]
        public void Should_Load_Embedded_Fallbacks()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                using (AvaloniaLocator.EnterScope())
                {
                    var fontFamily = FontFamily.Parse("NotFound, " + s_fontUri);

                    var typeface = new Typeface(fontFamily);

                    var glyphTypeface = typeface.GlyphTypeface;

                    Assert.NotNull(glyphTypeface);

                    Assert.Equal("Noto Mono", glyphTypeface.FamilyName);
                }
            }
        }

        [Fact]
        public void Should_Match_Chararcter_Width_Embedded_Fallbacks()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                using (AvaloniaLocator.EnterScope())
                {
                    var fontFamily = FontFamily.Parse("NotFound, " + s_fontUri);

                    Assert.True(FontManager.Current.TryMatchCharacter('A', FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, fontFamily, null, out var typeface));

                    var glyphTypeface = typeface.GlyphTypeface;

                    Assert.NotNull(glyphTypeface);

                    Assert.Equal("Noto Mono", glyphTypeface.FamilyName);
                }
            }
        }

        [Fact]
        public void Should_Match_Chararcter_From_SystemFonts()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                using (AvaloniaLocator.EnterScope())
                {
                    Assert.True(FontManager.Current.TryMatchCharacter('A', FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, null, null, out var typeface));

                    var glyphTypeface = typeface.GlyphTypeface;

                    Assert.NotNull(glyphTypeface);

                    Assert.Equal(FontManager.Current.DefaultFontFamily.Name, glyphTypeface.FamilyName);
                }
            }
        }

        [Fact]
        public void Should_Match_Chararcter_Width_Fallbacks()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                using (AvaloniaLocator.EnterScope())
                {
                    var fontFamily = FontFamily.Parse("NotFound, Unknown");

                    Assert.True(FontManager.Current.TryMatchCharacter('A', FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, fontFamily, null, out var typeface));

                    var glyphTypeface = typeface.GlyphTypeface;

                    Assert.NotNull(glyphTypeface);

                    Assert.Equal(FontManager.Current.DefaultFontFamily.Name, glyphTypeface.FamilyName);
                }
            }
        }

        [Fact]
        public void Should_Use_Custom_SystemFont()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                using (AvaloniaLocator.EnterScope())
                {
                    var systemFontCollection = FontManager.Current.SystemFonts as SystemFontCollection;

                    Assert.NotNull(systemFontCollection);

                    systemFontCollection.AddCustomFontSource(new Uri(s_fontUri, UriKind.Absolute));

                    Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface("Noto Mono"), out var glyphTypeface));

                    Assert.Equal("Noto Mono", glyphTypeface.FamilyName);
                }
            }
        }


        [Fact]
        public void Should_Get_Nearest_Match_For_Custom_SystemFont()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                using (AvaloniaLocator.EnterScope())
                {
                    var systemFontCollection = FontManager.Current.SystemFonts as SystemFontCollection;

                    Assert.NotNull(systemFontCollection);

                    systemFontCollection.AddCustomFontSource(new Uri(s_fontUri, UriKind.Absolute));

                    Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface("Noto Mono", FontStyle.Italic), out var glyphTypeface));

                    Assert.Equal("Noto Mono", glyphTypeface.FamilyName);
                }
            }
        }
    }
}
