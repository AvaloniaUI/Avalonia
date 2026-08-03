using System;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class FontManagerTryGetFontCollectionTests
    {
        [Fact]
        public void TryGetFontCollection_SystemFontScheme_ReturnsTrue()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var source = new Uri($"{FontManager.SystemFontScheme}:Arial", UriKind.Absolute);

                Assert.True(FontManager.Current.TryGetFontCollection(source, out var collection));
                Assert.NotNull(collection);
            }
        }

        [Fact]
        public void TryGetFontCollection_SystemFontScheme_YieldsSystemFontCollection()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var source = new Uri($"{FontManager.SystemFontScheme}:Arial", UriKind.Absolute);

                FontManager.Current.TryGetFontCollection(source, out var collection);

                Assert.IsType<SystemFontCollection>(collection);
            }
        }

        [Fact]
        public void TryGetFontCollection_SystemFontScheme_ReturnsSameInstanceOnSubsequentCalls()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var source = new Uri($"{FontManager.SystemFontScheme}:Arial", UriKind.Absolute);
                var fm = FontManager.Current;

                fm.TryGetFontCollection(source, out var first);
                fm.TryGetFontCollection(source, out var second);

                Assert.Same(first, second);
            }
        }

        [Fact]
        public void TryGetFontCollection_SystemFontsKey_ReturnsTrue()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                Assert.True(FontManager.Current.TryGetFontCollection(FontManager.SystemFontsKey, out var collection));
                Assert.NotNull(collection);
            }
        }

        [Fact]
        public void TryGetFontCollection_SystemFontsKey_YieldsSystemFontCollection()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                FontManager.Current.TryGetFontCollection(FontManager.SystemFontsKey, out var collection);

                Assert.IsType<SystemFontCollection>(collection);
            }
        }

        [Fact]
        public void TryGetFontCollection_SystemFontSchemeAndSystemFontsKey_ReturnSameInstance()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var fm = FontManager.Current;
                var schemeSource = new Uri($"{FontManager.SystemFontScheme}:Arial", UriKind.Absolute);

                fm.TryGetFontCollection(schemeSource, out var fromScheme);
                fm.TryGetFontCollection(FontManager.SystemFontsKey, out var fromKey);

                Assert.Same(fromScheme, fromKey);
            }
        }

        [Fact]
        public void TryGetFontCollection_RegisteredFontsCollection_ReturnsTrue()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var key = new Uri("fonts:MyTest", UriKind.Absolute);
                var stub = new StubFontCollection(key);
                var fm = FontManager.Current;
                fm.AddFontCollection(stub);

                Assert.True(fm.TryGetFontCollection(key, out var collection));
                Assert.Same(stub, collection);
            }
        }

        [Fact]
        public void TryGetFontCollection_UnregisteredFontsCollection_ReturnsFalse()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var key = new Uri("fonts:DoesNotExist", UriKind.Absolute);

                Assert.False(FontManager.Current.TryGetFontCollection(key, out var collection));
                Assert.Null(collection);
            }
        }

        [Fact]
        public void TryGetFontCollection_UnregisteredFontsCollection_DoesNotCacheNull()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var key = new Uri("fonts:DoesNotExist2", UriKind.Absolute);
                var fm = FontManager.Current;

                // First call returns false
                Assert.False(fm.TryGetFontCollection(key, out _));

                // Register after the first failed lookup
                var stub = new StubFontCollection(key);
                fm.AddFontCollection(stub);

                // Now it should be found if null had been cached this would still fail
                Assert.True(fm.TryGetFontCollection(key, out var collection));
                Assert.Same(stub, collection);
            }
        }

        [Fact]
        public void TryGetFontCollection_AbsoluteResm_ReturnsTrue()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var source = new Uri("resm:Avalonia.Base.UnitTests.Assets?assembly=Avalonia.Base.UnitTests", UriKind.Absolute);

                Assert.True(FontManager.Current.TryGetFontCollection(source, out var collection));
                Assert.NotNull(collection);
            }
        }

        [Fact]
        public void TryGetFontCollection_AbsoluteResm_YieldsEmbeddedFontCollection()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var source = new Uri("resm:Avalonia.Base.UnitTests.Assets?assembly=Avalonia.Base.UnitTests", UriKind.Absolute);

                FontManager.Current.TryGetFontCollection(source, out var collection);

                Assert.IsType<EmbeddedFontCollection>(collection);
            }
        }

        [Fact]
        public void TryGetFontCollection_AbsoluteResm_ReturnsSameInstanceOnSubsequentCalls()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var source = new Uri("resm:Avalonia.Base.UnitTests.Assets?assembly=Avalonia.Base.UnitTests", UriKind.Absolute);
                var fm = FontManager.Current;

                fm.TryGetFontCollection(source, out var first);
                fm.TryGetFontCollection(source, out var second);

                Assert.Same(first, second);
            }
        }

        [Fact]
        public void TryGetFontCollection_Avares_ReturnsTrue()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var source = new Uri("avares://Avalonia.Base.UnitTests/Assets", UriKind.Absolute);

                Assert.True(FontManager.Current.TryGetFontCollection(source, out var collection));
                Assert.NotNull(collection);
            }
        }

        [Fact]
        public void TryGetFontCollection_Avares_YieldsEmbeddedFontCollection()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var source = new Uri("avares://Avalonia.Base.UnitTests/Assets", UriKind.Absolute);

                FontManager.Current.TryGetFontCollection(source, out var collection);

                Assert.IsType<EmbeddedFontCollection>(collection);
            }
        }

        [Fact]
        public void TryGetFontCollection_Avares_ReturnsSameInstanceOnSubsequentCalls()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var source = new Uri("avares://Avalonia.Base.UnitTests/Assets", UriKind.Absolute);
                var fm = FontManager.Current;

                fm.TryGetFontCollection(source, out var first);
                fm.TryGetFontCollection(source, out var second);

                Assert.Same(first, second);
            }
        }

        [Fact]
        public void TryGetFontCollection_UnknownScheme_ReturnsFalse()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var source = new Uri("https://example.com/fonts", UriKind.Absolute);

                Assert.False(FontManager.Current.TryGetFontCollection(source, out var collection));
                Assert.Null(collection);
            }
        }

        [Fact]
        public void TryGetFontCollection_UnknownScheme_DoesNotCacheNull()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                // Verify that repeated lookups for the same unknown-scheme URI
                // consistently return false/null rather than succeeding due to an
                // accidentally cached null or invalid entry.
                var source = new Uri("file:///some/path/fonts", UriKind.Absolute);
                var fm = FontManager.Current;

                Assert.False(fm.TryGetFontCollection(source, out var first));
                Assert.Null(first);

                Assert.False(fm.TryGetFontCollection(source, out var second));
                Assert.Null(second);
            }
        }

        private sealed class StubFontCollection : IFontCollection
        {
            public StubFontCollection(Uri key) => Key = key;

            public Uri Key { get; }
            public int Count => 0;
            public FontFamily this[int index] => throw new NotSupportedException();
            public bool TryGetGlyphTypeface(string familyName, FontStyle style, FontWeight weight, FontStretch stretch, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out GlyphTypeface? glyphTypeface) { glyphTypeface = null; return false; }
            public bool TryMatchCharacter(int codepoint, FontStyle style, FontWeight weight, FontStretch stretch, string? familyName, System.Globalization.CultureInfo? culture, out Typeface typeface) { typeface = default; return false; }
            public bool TryGetFamilyTypefaces(string familyName, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out System.Collections.Generic.IReadOnlyList<Typeface>? familyTypefaces) { familyTypefaces = null; return false; }
            public bool TryCreateSyntheticGlyphTypeface(GlyphTypeface glyphTypeface, FontStyle style, FontWeight weight, FontStretch stretch, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out GlyphTypeface? syntheticGlyphTypeface) { syntheticGlyphTypeface = null; return false; }
            public bool TryGetNearestMatch(string familyName, FontStyle style, FontWeight weight, FontStretch stretch, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out GlyphTypeface? glyphTypeface) { glyphTypeface = null; return false; }
            public System.Collections.Generic.IEnumerator<FontFamily> GetEnumerator() => System.Linq.Enumerable.Empty<FontFamily>().GetEnumerator();
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
            public void Dispose() { }
        }
    }
}
