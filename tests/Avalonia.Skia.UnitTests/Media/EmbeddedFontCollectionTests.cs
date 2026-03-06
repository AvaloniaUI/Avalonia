#nullable enable

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media
{
    public class EmbeddedFontCollectionTests
    {
        private const string s_fontAssets =
            "resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests";

        [InlineData(FontWeight.SemiLight, FontStyle.Normal)]
        [InlineData(FontWeight.Bold, FontStyle.Italic)]
        [InlineData(FontWeight.Heavy, FontStyle.Oblique)]
        [Theory]
        public void Should_Get_Near_Matching_Typeface(FontWeight fontWeight, FontStyle fontStyle)
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new CustomFontManagerImpl())))
            {
                var key = new Uri("fonts:testFonts", UriKind.Absolute);
                var source = new Uri(s_fontAssets, UriKind.Absolute);

                var fontCollection = new TestEmbeddedFontCollection(source, source);

                Assert.True(fontCollection.TryGetGlyphTypeface("Noto Mono", fontStyle, fontWeight, FontStretch.Normal, out var glyphTypeface));

                var actual = glyphTypeface.FamilyName;

                Assert.Equal("Noto Mono", actual);
            }
        }
        
        [Fact]
        public void Should_Not_Get_Typeface_For_Invalid_FamilyName()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new CustomFontManagerImpl())))
            {
                var key = new Uri("fonts:testFonts", UriKind.Absolute);
                var source = new Uri(s_fontAssets, UriKind.Absolute);

                var fontCollection = new TestEmbeddedFontCollection(key, source);

                Assert.False(fontCollection.TryGetGlyphTypeface("ABC", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, out _));
            }
        }

        [Fact]
        public void Should_Get_Typeface_For_Partial_FamilyName()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new CustomFontManagerImpl())))
            {
                var key = new Uri("fonts:testFonts", UriKind.Absolute);
                var source = new Uri(s_fontAssets, UriKind.Absolute);

                var fontCollection = new TestEmbeddedFontCollection(key, source);

                Assert.True(fontCollection.TryGetGlyphTypeface("T", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, out var glyphTypeface));

                Assert.Equal("Twitter Color Emoji", glyphTypeface.FamilyName);
            }
        }

        [Fact]
        public void Should_Get_Typeface_For_TypographicFamilyName()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new CustomFontManagerImpl())))
            {
                var key = new Uri("fonts:testFonts", UriKind.Absolute);
                var source = new Uri(s_fontAssets, UriKind.Absolute);

                var fontCollection = new TestEmbeddedFontCollection(key, source);

                Assert.True(fontCollection.TryGetGlyphTypeface("Manrope", FontStyle.Normal, FontWeight.Light, FontStretch.Normal, out var glyphTypeface));

                Assert.Equal("Manrope Light", glyphTypeface.FamilyName);

                Assert.Equal("Manrope", glyphTypeface.TypographicFamilyName);
            }
        }

        [Fact]
        public void Should_Cache_Synthetic_GlyphTypeface()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new CustomFontManagerImpl())))
            {
                var key = new Uri("fonts:testFonts", UriKind.Absolute);
                var source = new Uri(s_fontAssets, UriKind.Absolute);

                var fontCollection = new TestEmbeddedFontCollection(key, source, true);

                Assert.True(fontCollection.TryGetGlyphTypeface("Manrope", FontStyle.Normal, FontWeight.ExtraBlack, FontStretch.Normal, out var glyphTypeface));

                Assert.True(fontCollection.GlyphTypefaceCache.TryGetValue("Manrope", out var glyphTypefaces));

                Assert.Equal(2, glyphTypefaces.Count);

                fontCollection.TryGetGlyphTypeface("Manrope", FontStyle.Normal, FontWeight.ExtraBlack, FontStretch.Normal, out var otherGlyphTypeface);

                Assert.Equal(glyphTypeface, otherGlyphTypeface);
            }
        }

        [Fact]
        public void Should_Cache_Nearest_Match_For_MiSans()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                var source = new Uri(s_fontAssets, UriKind.Absolute);

                var fontCollection = new TestEmbeddedFontCollection(source, source);

                // Font weight 304
                Assert.True(fontCollection.TryGetGlyphTypeface("MiSans", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, out var regularGlyphTypeface));

                // Font weight regular (400)
                Assert.True(fontCollection.TryGetGlyphTypeface("MiSans", FontStyle.Normal, FontWeight.Bold, FontStretch.Normal, out var boldGlyphTypeface));

                // Font weight 700
                Assert.True(fontCollection.GlyphTypefaceCache.TryGetValue("MiSans", out var glyphTypefaces));

                Assert.Equal(3, glyphTypefaces.Count);
            }
        }

        private class TestEmbeddedFontCollection : EmbeddedFontCollection
        {
            private bool _createSyntheticTypefaces;

            public TestEmbeddedFontCollection(Uri key, Uri source, bool createSyntheticTypefaces = false) : base(key, source)
            {
                _createSyntheticTypefaces = createSyntheticTypefaces;
            }

            public IDictionary<string, ConcurrentDictionary<FontCollectionKey, GlyphTypeface?>> GlyphTypefaceCache => _glyphTypefaceCache;

            public override bool TryCreateSyntheticGlyphTypeface(
               GlyphTypeface glyphTypeface,
               FontStyle style, 
               FontWeight weight,
               FontStretch stretch,
               [NotNullWhen(true)] out GlyphTypeface? syntheticGlyphTypeface)
            {
                if (!_createSyntheticTypefaces)
                {
                    syntheticGlyphTypeface = null;

                    return false;
                }

                return base.TryCreateSyntheticGlyphTypeface(glyphTypeface, style, weight, stretch, out syntheticGlyphTypeface);
            }
        }
    }
}
