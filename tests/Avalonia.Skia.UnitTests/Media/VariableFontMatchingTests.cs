using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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

        [Theory]
        [InlineData(925)]
        [InlineData(975)]
        public void Weight_Beyond_The_Axis_Range_Off_The_50_Grid_Uses_The_Axis_Maximum(int weight)
        {
            using (Start(out var root))
            {
                var glyphTypeface = Resolve(new Typeface(InterVariable, weight: (FontWeight)weight));

                Assert.Equal(FontSimulations.Bold, glyphTypeface.FontSimulations);
                Assert.Equal(Position(root, "wght=900"), glyphTypeface.VariationPosition);
            }
        }

        [Fact]
        public void Weight_Below_The_Axis_Range_Off_The_50_Grid_Uses_The_Axis_Minimum()
        {
            using (Start(out var root))
            {
                var glyphTypeface = Resolve(new Typeface(InterVariable, weight: (FontWeight)75));

                Assert.Equal(FontSimulations.None, glyphTypeface.FontSimulations);
                Assert.Equal(Position(root, "wght=100"), glyphTypeface.VariationPosition);
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
        public void Resolved_Varied_Face_Is_An_Exact_Match_For_A_Later_Character_Lookup()
        {
            using (Start(out _, out var collection))
            {
                var resolved = Resolve(new Typeface(InterVariable, weight: FontWeight.SemiBold));

                // The wght=600 clone registered for SemiBold already is the requested face, so the
                // fallback must neither ask the platform for an exact face nor replace the clone.
                Assert.True(collection.TryMatchCharacter('A', FontStyle.Normal, FontWeight.SemiBold,
                    FontStretch.Normal, "Inter Variable", null, out var match));

                Assert.Equal(0, collection.PlatformCallCount);

                var glyphTypeface = Resolve(match);

                Assert.Same(resolved, glyphTypeface);
                Assert.Equal(FontSimulations.None, glyphTypeface.FontSimulations);
            }
        }

        [Theory]
        [InlineData(450)] // between named instances: reached along the weight axis
        [InlineData(700)] // a named instance weight, but the instance sits at the default optical size
        public void Family_At_An_Optical_Size_Keeps_It_For_A_Requested_Weight(int weight)
        {
            using (Start(out var root, out var collection))
            {
                // A family whose only face is Inter Variable at its largest optical size, the way an
                // optical-size instance is exposed as a family of its own.
                collection.AddFamily("Inter Display", root.WithVariations(FontVariationSettings.Parse("opsz=32")));

                var glyphTypeface = Resolve(new Typeface(CollectionKey + "#Inter Display", weight: (FontWeight)weight));

                Assert.Equal(FontSimulations.None, glyphTypeface.FontSimulations);
                Assert.Equal((FontWeight)weight, glyphTypeface.Weight);
                Assert.Equal(Position(root, $"opsz=32,wght={weight}"), glyphTypeface.VariationPosition);
            }
        }

        [Fact]
        public void Family_Of_A_Varied_Clone_Projects_Named_Instances_From_The_Source()
        {
            using (Start(out var root, out var collection))
            {
                // The family holds only the wght=700 clone, so its named instances are matched
                // from the clone. The Regular instance sits at the default position and draws
                // wght 400, whatever the clone reports.
                collection.AddFamily("Inter Clone", root.WithVariations(FontVariationSettings.Parse("wght=700")));

                var glyphTypeface = Resolve(new Typeface(CollectionKey + "#Inter Clone", weight: FontWeight.Bold));

                Assert.Equal(FontSimulations.None, glyphTypeface.FontSimulations);
                Assert.Equal(FontWeight.Bold, glyphTypeface.Weight);
                Assert.Equal(Position(root, "wght=700"), glyphTypeface.VariationPosition);
            }
        }

        [Fact]
        public void Slant_Axis_That_Cannot_Reach_Upright_Is_Not_An_Exact_Normal_Match()
        {
            using (Start(out _, out var collection))
            {
                // A variable face slanted at every position (slnt -15 .. -5) and a static upright
                // Bold face. A Normal request prefers the upright face over a slanted position.
                collection.AddFamily("Mixed", CreateAlwaysSlantedFace());
                collection.AddFamily("Mixed", Resolve(new Typeface(CollectionKey + "#Inter", weight: FontWeight.Bold)));

                var glyphTypeface = Resolve(new Typeface(CollectionKey + "#Mixed", weight: FontWeight.SemiBold));

                Assert.Equal(FontStyle.Normal, glyphTypeface.Style);
                Assert.Equal(FontWeight.Bold, glyphTypeface.Weight);
                Assert.Empty(glyphTypeface.VariationAxes);
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

        /// <summary>
        /// Inter Variable with its fvar replaced by a slnt axis of -15 .. -5 (default -5) and
        /// a wght axis of 100 .. 900, and no named instances.
        /// </summary>
        private static GlyphTypeface CreateAlwaysSlantedFace()
        {
            var assetLoader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();

            using var stream = assetLoader.Open(
                new Uri($"resm:{AssetsNamespace}.InterVariable.ttf?assembly=Avalonia.Skia.UnitTests"));

            var fvar = new BigEndianBuffer()
                .UInt16(1)   // majorVersion
                .UInt16(0)   // minorVersion
                .UInt16(16)  // axesArrayOffset
                .UInt16(2)   // reserved
                .UInt16(2)   // axisCount
                .UInt16(20)  // axisSize
                .UInt16(0)   // instanceCount
                .UInt16(0);  // instanceSize

            fvar.Tag("slnt").Fixed(-15).Fixed(-5).Fixed(-5).UInt16(0).UInt16(0);
            fvar.Tag("wght").Fixed(100).Fixed(400).Fixed(900).UInt16(0).UInt16(0);

            return SyntheticFont.FromStream(stream)
                .Replace("fvar", fvar.ToArray())
                .Remove("avar")
                .CreateGlyphTypeface();
        }

        private static IDisposable Start(out GlyphTypeface interVariable) => Start(out interVariable, out _);

        private static IDisposable Start(out GlyphTypeface interVariable, out VariableFontCollection collection)
        {
            var app = UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl()));

            collection = new VariableFontCollection(new Uri(CollectionKey, UriKind.Absolute));
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

            public int PlatformCallCount { get; private set; }

            public void AddFamily(string familyName, GlyphTypeface glyphTypeface)
                => TryAddGlyphTypeface(familyName, glyphTypeface.ToFontCollectionKey(), glyphTypeface);

            protected override bool TryMatchCharacterFromPlatform(int codepoint, FontCollectionKey key,
                string? familyName, CultureInfo? culture, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
            {
                PlatformCallCount++;
                glyphTypeface = null;
                return false;
            }
        }
    }
}
