#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media
{
    /// <summary>
    /// Verifies that <see cref="FontCollectionBase"/>.TryMatchCharacter resolves the same family
    /// regardless of the order in which fonts were added to the collection, and is stable
    /// across repeated invocations.
    /// </summary>
    public class FontCollectionDeterminismTests
    {
        private const string AssetsNamespace = "Avalonia.Skia.UnitTests.Assets";

        // A set of Latin-covering test fonts. All cover ASCII 'A'.
        private static readonly string[] s_latinFontAssets =
        {
            $"{AssetsNamespace}.Inter-Regular.ttf",
            $"{AssetsNamespace}.Inter-Bold.ttf",
            $"{AssetsNamespace}.Manrope-Light.ttf",
            $"{AssetsNamespace}.NotoMono-Regular.ttf",
            $"{AssetsNamespace}.NotoSans-Italic.ttf",
            $"{AssetsNamespace}.SourceSerif4_36pt-Italic.ttf",
        };

        [Fact]
        public void TryMatchCharacter_Returns_Same_Family_Regardless_Of_Add_Order()
        {
            using var app = UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl()));

            var orderings = new[]
            {
                s_latinFontAssets,
                s_latinFontAssets.Reverse().ToArray(),
                Shuffle(s_latinFontAssets, seed: 1),
                Shuffle(s_latinFontAssets, seed: 17),
                Shuffle(s_latinFontAssets, seed: 42),
                Shuffle(s_latinFontAssets, seed: 1337),
            };

            string? expected = null;

            foreach (var ordering in orderings)
            {
                var collection = BuildCollection(ordering);

                Assert.True(collection.TryMatchCharacter(
                    'A', FontStyle.Normal, FontWeight.Normal, FontStretch.Normal,
                    familyName: null, culture: null, out var match));

                var familyName = ExtractFamilyName(match);

                if (expected is null)
                {
                    expected = familyName;
                }
                else
                {
                    Assert.Equal(expected, familyName);
                }
            }
        }

        [Fact]
        public void TryMatchCharacter_Is_Stable_Across_Repeated_Invocations()
        {
            using var app = UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl()));

            var collection = BuildCollection(s_latinFontAssets);

            Assert.True(collection.TryMatchCharacter(
                'A', FontStyle.Normal, FontWeight.Normal, FontStretch.Normal,
                familyName: null, culture: null, out var firstMatch));

            var expected = ExtractFamilyName(firstMatch);

            for (var i = 0; i < 50; i++)
            {
                Assert.True(collection.TryMatchCharacter(
                    'A', FontStyle.Normal, FontWeight.Normal, FontStretch.Normal,
                    familyName: null, culture: null, out var match));

                Assert.Equal(expected, ExtractFamilyName(match));
            }
        }

        [Fact]
        public void TryMatchCharacter_Result_Is_Independent_Of_Concurrent_Cache_Population()
        {
            using var app = UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl()));

            // Touch a variety of typefaces in different orders before the fallback call,
            // so each collection's _glyphTypefaceCache has different ConcurrentDictionary
            // insertion / hash-bucket order.
            var resultsA = new List<string>();
            var resultsB = new List<string>();

            for (var i = 0; i < 5; i++)
            {
                var a = BuildCollection(s_latinFontAssets);
                var b = BuildCollection(s_latinFontAssets);

                // Different warm-up order on purpose.
                Warmup(a, new[] { "Inter", "Manrope Light", "Noto Mono", "Noto Sans", "Source Serif 4 36pt" });
                Warmup(b, new[] { "Source Serif 4 36pt", "Noto Sans", "Noto Mono", "Manrope Light", "Inter" });

                Assert.True(a.TryMatchCharacter('A', FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, null, null, out var ma));
                Assert.True(b.TryMatchCharacter('A', FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, null, null, out var mb));

                resultsA.Add(ExtractFamilyName(ma));
                resultsB.Add(ExtractFamilyName(mb));
            }

            // Every iteration of A produces the same family.
            Assert.Single(resultsA.Distinct());

            // Every iteration of B produces the same family.
            Assert.Single(resultsB.Distinct());

            // And the chosen family is the same for both warm-up orders.
            Assert.Equal(resultsA[0], resultsB[0]);
        }

        [Fact]
        public void TryMatchCharacter_Cached_ScriptFallback_Lookup_Returns_Same_Family()
        {
            using var app = UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl()));

            var collection = BuildCollection(s_latinFontAssets);

            // First call populates the script/culture fallback cache; subsequent calls must hit
            // it and still return the same family.
            Assert.True(collection.TryMatchCharacter(
                'A', FontStyle.Normal, FontWeight.Normal, FontStretch.Normal,
                familyName: null, culture: null, out var first));

            Assert.True(collection.TryMatchCharacter(
                'A', FontStyle.Normal, FontWeight.Normal, FontStretch.Normal,
                familyName: null, culture: null, out var second));

            Assert.Equal(ExtractFamilyName(first), ExtractFamilyName(second));
        }

        private static CustomFontCollection BuildCollection(IEnumerable<string> assetPaths)
        {
            var assetLoader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();
            var collection = new CustomFontCollection(new Uri("fonts:determinism", UriKind.Absolute));

            foreach (var path in assetPaths)
            {
                var uri = new Uri($"resm:{path}?assembly=Avalonia.Skia.UnitTests", UriKind.Absolute);

                using var stream = assetLoader.Open(uri);

                Assert.True(collection.TryAddGlyphTypeface(stream, out _));
            }

            return collection;
        }

        private static void Warmup(CustomFontCollection collection, IEnumerable<string> familyNames)
        {
            foreach (var name in familyNames)
            {
                collection.TryGetGlyphTypeface(name, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, out _);
            }
        }

        private static string[] Shuffle(string[] source, int seed)
        {
            var rng = new Random(seed);
            var copy = (string[])source.Clone();

            for (var i = copy.Length - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (copy[i], copy[j]) = (copy[j], copy[i]);
            }

            return copy;
        }

        private static string ExtractFamilyName(Typeface typeface)
        {
            // Fallback Typefaces are built with a FontFamily of the form "<collection-key>#<familyName>".
            // The plain family name is what we want to compare across orderings.
            var name = typeface.FontFamily.Name;
            var hashIndex = name.LastIndexOf('#');
            return hashIndex >= 0 ? name[(hashIndex + 1)..] : name;
        }

        private sealed class CustomFontCollection(Uri key) : FontCollectionBase
        {
            public override Uri Key { get; } = key;
        }
    }
}
