using System;
using System.Collections.Generic;
using Avalonia.Fonts.Inter;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Media.TextFormatting.Unicode;
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
            var fontManagerImpl = new TestFontManager();

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManagerImpl)))
            {
                Assert.True(FontManager.Current.TryGetGlyphTypeface(Typeface.Default, out _));

                var countBefore = fontManagerImpl.TryCreateGlyphTypefaceCount;

                for (int i = 0; i < 10; i++)
                {
                    FontManager.Current.TryGetGlyphTypeface(new Typeface("Unknown"), out _);
                }

                Assert.Equal(countBefore + 1, fontManagerImpl.TryCreateGlyphTypefaceCount);
            }
        }

        [Fact]
        public void Should_Cache_MatchCharacter()
        {
            var fontManagerImpl = new CustomFontManagerImpl();

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: fontManagerImpl)))
            {
                var emoji = Codepoint.ReadAt("😀", 0, out _);

                Assert.True(FontManager.Current.TryMatchCharacter((int)emoji, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, null, null, out var firstMatch));

                var firstGlyphTypeface = firstMatch.GlyphTypeface;

                Assert.True(FontManager.Current.TryMatchCharacter((int)emoji, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, null, null, out var secondMatch));

                var secondGlyphTypeface = secondMatch.GlyphTypeface;

                Assert.Equal(firstGlyphTypeface, secondGlyphTypeface);
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
                    Assert.NotNull(glyphTypeface);
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

        [Theory]
        [InlineData("NotFound, Unknown", null)] // system fonts
        [InlineData("/#NotFound, /#Unknown", "avares://some/path")] // embedded fonts
        public void Should_Match_Character_With_Fallbacks(string familyName, string? baseUri)
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                using (AvaloniaLocator.EnterScope())
                {
                    var fontFamily = FontFamily.Parse(familyName, baseUri is null ? null : new Uri(baseUri));

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
                    FontManager.Current.AddFontCollection(new EmbeddedFontCollection(FontManager.SystemFontsKey,
                        new Uri(s_fontUri, UriKind.Absolute)));

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
                    FontManager.Current.AddFontCollection(new EmbeddedFontCollection(FontManager.SystemFontsKey,
                        new Uri(s_fontUri, UriKind.Absolute)));

                    Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface("Noto Mono", FontStyle.Italic), out var glyphTypeface));

                    Assert.Equal("Noto Mono", glyphTypeface.FamilyName);
                }
            }
        }

        [Fact]
        public void Should_Get_Implicit_Typeface()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                using (AvaloniaLocator.EnterScope())
                {
                    FontManager.Current.AddFontCollection(new EmbeddedFontCollection(FontManager.SystemFontsKey,
                        new Uri(s_fontUri, UriKind.Absolute)));

                    Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface("Noto Mono Italic"),
                        out var glyphTypeface));

                    Assert.Equal("Noto Mono", glyphTypeface.FamilyName);

                    Assert.Equal(FontStyle.Italic, glyphTypeface.Style);
                }
            }
        }

        [Fact]
        public void Should_Create_Synthetic_Typeface()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                using (AvaloniaLocator.EnterScope())
                {
                    FontManager.Current.AddFontCollection(new EmbeddedFontCollection(FontManager.SystemFontsKey,
                        new Uri(s_fontUri, UriKind.Absolute)));

                    Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface("Noto Mono", FontStyle.Italic, FontWeight.Bold),
                        out var italicBoldTypeface));

                    Assert.Equal("Noto Mono", italicBoldTypeface.FamilyName);

                    Assert.True(italicBoldTypeface.PlatformTypeface.FontSimulations.HasFlag(FontSimulations.Bold));

                    Assert.True(italicBoldTypeface.PlatformTypeface.FontSimulations.HasFlag(FontSimulations.Oblique));

                    Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface("Noto Mono", FontStyle.Normal, FontWeight.Normal),
                       out var regularTypeface));

                    Assert.NotEqual(((SkiaTypeface)regularTypeface.PlatformTypeface).SKTypeface, ((SkiaTypeface)italicBoldTypeface.PlatformTypeface).SKTypeface);
                }
            }
        }

        [Win32Fact("Requires Windows Fonts")]
        public void Should_Get_GlyphTypeface_By_Localized_FamilyName()
        {
            using (UnitTestApplication.Start(
                       TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                using (AvaloniaLocator.EnterScope())
                {
                    Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface("微軟正黑體"), out var glyphTypeface));

                    Assert.Equal("Microsoft JhengHei", glyphTypeface.FamilyName);
                }
            }
        }

        [Fact]
        public void Should_Get_FontFeatures()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                using (AvaloniaLocator.EnterScope())
                {
                    FontManager.Current.AddFontCollection(new InterFontCollection());

                    Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface("fonts:Inter#Inter"),
                        out var glyphTypeface));

                    Assert.Equal("Inter", glyphTypeface.FamilyName);

                    var features = glyphTypeface.SupportedFeatures;

                    Assert.NotEmpty(features);
                }
            }
        }

        [Fact]
        public void Should_Map_FontFamily()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                using (AvaloniaLocator.EnterScope())
                {
                    AvaloniaLocator.CurrentMutable.BindToSelf(new FontManagerOptions
                    {
                        DefaultFamilyName = s_fontUri,
                        FontFamilyMappings = new Dictionary<string, FontFamily>
                        {
                            { "Segoe UI", new FontFamily("fonts:Inter#Inter") }
                        }
                    });

                    FontManager.Current.AddFontCollection(new InterFontCollection());

                    var result = FontManager.Current.TryGetGlyphTypeface(new Typeface("Abc, Segoe UI"), out var glyphTypeface);

                    Assert.True(result);
                    Assert.NotNull(glyphTypeface);
                    Assert.Equal("Inter", glyphTypeface.FamilyName);
                }
            }
        }

        [Fact]
        public void Should_Get_FamilyTypefaces()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                using (AvaloniaLocator.EnterScope())
                {
                    FontManager.Current.AddFontCollection(new InterFontCollection());

                    var familyTypefaces = FontManager.Current.GetFamilyTypefaces(new FontFamily("fonts:Inter#Inter"));

                    Assert.Equal(6, familyTypefaces.Count);
                }
            }
        }

        [Fact]
        public void Should_Use_FontCollection_MatchCharacter()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                using (AvaloniaLocator.EnterScope())
                {
                    FontManager.Current.AddFontCollection(
                        new EmbeddedFontCollection(
                            new Uri("fonts:MyCollection"), //key
                            new Uri("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests"))); //source

                    var fontFamily = new FontFamily("fonts:MyCollection#Noto Mono");

                    var character = "א";

                    var codepoint = Codepoint.ReadAt(character, 0, out _);

                    Assert.True(FontManager.Current.TryMatchCharacter(codepoint, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, fontFamily, null, out var typeface));

                    //Typeface should come from the font collection
                    Assert.NotNull(typeface.FontFamily.Key);

                    Assert.Equal("Noto Sans Hebrew", typeface.GlyphTypeface.FamilyName);
                }
            }
        }

        [InlineData("Arial")]
        [InlineData("#Arial")]
        [Win32Theory("Windows specific font")]
        public void Should_Get_SystemFont_With_BaseUri(string name)
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                using (AvaloniaLocator.EnterScope())
                {
                    var fontFamily = new FontFamily(new Uri("avares://Avalonia.Skia.UnitTests/NotFound"), name);

                    var glyphTypeface = new Typeface(fontFamily).GlyphTypeface;

                    Assert.Equal("Arial", glyphTypeface.FamilyName);
                }
            }
        }


        [Win32Fact("Windows specific font")]
        public void Should_Get_Regular_Font_After_Matching_Italic_Font()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                using (AvaloniaLocator.EnterScope())
                {
                    Assert.True(FontManager.Current.TryMatchCharacter('こ', FontStyle.Italic, FontWeight.Normal, FontStretch.Normal, null, null, out var italicTypeface));

                    Assert.Equal(FontSimulations.None, italicTypeface.GlyphTypeface.FontSimulations);

                    Assert.Equal("Yu Gothic UI", italicTypeface.GlyphTypeface.FamilyName);

                    Assert.NotEqual(FontStyle.Normal, italicTypeface.Style);

                    Assert.True(FontManager.Current.TryMatchCharacter('こ', FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, null, null, out var regularTypeface));

                    Assert.Equal("Yu Gothic UI", regularTypeface.GlyphTypeface.FamilyName);

                    Assert.Equal(FontStyle.Normal, regularTypeface.Style);

                    Assert.NotEqual(((SkiaTypeface)italicTypeface.GlyphTypeface.PlatformTypeface).SKTypeface, ((SkiaTypeface)regularTypeface.GlyphTypeface.PlatformTypeface).SKTypeface);
                }
            }
        }

        [Fact]
        public void Should_Fallback_When_Font_Family_Is_Empty()
        {
            using (UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                using (AvaloniaLocator.EnterScope())
                {
                    var typeface = new Typeface(string.Empty);
                    Assert.NotNull(typeface.FontFamily);
                }
            }
        }
    }
}
