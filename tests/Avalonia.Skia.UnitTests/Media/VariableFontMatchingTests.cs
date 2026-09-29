using System;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media
{
    /// <summary>
    /// Font matching on a variable face reaches a requested weight, width or style along the
    /// face's axes before it falls back to the nearest face and simulation.
    /// </summary>
    public class VariableFontMatchingTests
    {
        private const string AssetsNamespace = "Avalonia.Skia.UnitTests.Assets";
        private const string CollectionKey = "fonts:variable";
        private const string InterVariable = CollectionKey + "#Inter Variable";

        private static readonly OpenTypeTag s_wght = OpenTypeTag.Parse("wght");
        private static readonly OpenTypeTag s_opsz = OpenTypeTag.Parse("opsz");

        [Fact]
        public void SemiBold_Request_Resolves_The_SemiBold_Position_Without_Simulation()
        {
            using (Start(out var root))
            {
                var glyphTypeface = Resolve(new Typeface(InterVariable, weight: FontWeight.SemiBold));

                Assert.Equal(FontSimulations.None, glyphTypeface.FontSimulations);
                Assert.Equal(FontWeight.SemiBold, glyphTypeface.Weight);
                Assert.Equal(Position(root, "wght=600"), glyphTypeface.VariationPosition);
            }
        }

        [Fact]
        public void Weight_Between_Named_Instances_Resolves_The_Axis_Value()
        {
            using (Start(out var root))
            {
                var glyphTypeface = Resolve(new Typeface(InterVariable, weight: (FontWeight)450));

                Assert.Equal(FontSimulations.None, glyphTypeface.FontSimulations);
                Assert.Equal((FontWeight)450, glyphTypeface.Weight);
                Assert.Equal(Position(root, "wght=450"), glyphTypeface.VariationPosition);
            }
        }

        [Fact]
        public void Weight_Beyond_The_Axis_Range_Simulates_Bold_Over_The_Axis_Maximum()
        {
            using (Start(out var root))
            {
                var glyphTypeface = Resolve(new Typeface(InterVariable, weight: FontWeight.ExtraBlack));

                Assert.Equal(FontSimulations.Bold, glyphTypeface.FontSimulations);
                Assert.Equal(Position(root, "wght=900"), glyphTypeface.VariationPosition);
            }
        }

        [Fact]
        public void Italic_Request_Simulates_Oblique_Over_The_Upright_Face()
        {
            // Inter Variable is upright only: no ital or slnt axis.
            using (Start(out _))
            {
                var glyphTypeface = Resolve(new Typeface(InterVariable, FontStyle.Italic));

                Assert.Equal(FontSimulations.Oblique, glyphTypeface.FontSimulations);
                Assert.True(glyphTypeface.VariationPosition.IsDefault);
            }
        }

        [Fact]
        public void Italic_SemiBold_Request_Simulates_Only_Oblique_Over_The_SemiBold_Position()
        {
            using (Start(out var root))
            {
                var glyphTypeface = Resolve(new Typeface(InterVariable, FontStyle.Italic, FontWeight.SemiBold));

                Assert.Equal(FontSimulations.Oblique, glyphTypeface.FontSimulations);
                Assert.Equal(FontWeight.SemiBold, glyphTypeface.Weight);
                Assert.Equal(Position(root, "wght=600"), glyphTypeface.VariationPosition);
            }
        }

        [Fact]
        public void Resolved_Varied_Face_Is_Cached_Under_The_Requested_Key()
        {
            using (Start(out _))
            {
                var first = Resolve(new Typeface(InterVariable, weight: FontWeight.SemiBold));
                var second = Resolve(new Typeface(InterVariable, weight: FontWeight.SemiBold));

                Assert.Same(first, second);
            }
        }

        [Fact]
        public void FontVariations_Override_Only_Their_Axes_Of_The_Resolved_Position()
        {
            using (Start(out var root))
            {
                var glyphTypeface = Resolve(new Typeface(InterVariable, FontStyle.Normal, FontWeight.SemiBold,
                    FontStretch.Normal, FontVariationSettings.Parse("opsz=32")));

                var semiBold = Position(root, "wght=600");

                Assert.Equal(semiBold.GetCoordinateOrDefault(s_wght),
                    glyphTypeface.VariationPosition.GetCoordinateOrDefault(s_wght));
                Assert.Equal(1f, glyphTypeface.VariationPosition.GetCoordinateOrDefault(s_opsz));
            }
        }

        [Fact]
        public void Static_Family_Still_Synthesizes_Bold()
        {
            using (Start(out _))
            {
                var glyphTypeface = Resolve(new Typeface(CollectionKey + "#Noto Mono", weight: FontWeight.Bold));

                Assert.Equal(FontSimulations.Bold, glyphTypeface.FontSimulations);
                Assert.Equal(FontWeight.Bold, glyphTypeface.Weight);
                Assert.True(glyphTypeface.VariationPosition.IsDefault);
            }
        }

        [Fact]
        public void Static_Family_Still_Prefers_A_Real_Face()
        {
            using (Start(out _))
            {
                var glyphTypeface = Resolve(new Typeface(CollectionKey + "#Inter", weight: FontWeight.Bold));

                Assert.Equal(FontSimulations.None, glyphTypeface.FontSimulations);
                Assert.Equal(FontWeight.Bold, glyphTypeface.Weight);
                Assert.Empty(glyphTypeface.VariationAxes);
            }
        }

        private static GlyphTypeface Resolve(Typeface typeface)
        {
            Assert.True(FontManager.Current.TryGetGlyphTypeface(typeface, out var glyphTypeface));

            return glyphTypeface;
        }

        private static NormalizedVariationPosition Position(GlyphTypeface root, string settings)
            => root.CreateNormalizedPosition(FontVariationSettings.Parse(settings));

        private static IDisposable Start(out GlyphTypeface interVariable)
        {
            var app = UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl()));

            var collection = new VariableFontCollection(new Uri(CollectionKey, UriKind.Absolute));
            FontManager.Current.AddFontCollection(collection);

            var assetLoader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();

            GlyphTypeface? variable = null;

            foreach (var asset in new[] { "InterVariable.ttf", "Inter-Regular.ttf", "Inter-Bold.ttf", "NotoMono-Regular.ttf" })
            {
                var uri = new Uri($"resm:{AssetsNamespace}.{asset}?assembly=Avalonia.Skia.UnitTests");

                using var stream = assetLoader.Open(uri);

                Assert.True(collection.TryAddGlyphTypeface(stream, out var glyphTypeface));

                variable ??= glyphTypeface;
            }

            interVariable = variable!;

            return app;
        }

        private sealed class VariableFontCollection(Uri key) : FontCollectionBase
        {
            public override Uri Key { get; } = key;
        }
    }
}
