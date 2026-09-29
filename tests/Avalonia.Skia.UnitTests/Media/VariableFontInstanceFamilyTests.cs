using System;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media
{
    /// <summary>
    /// The named instances of an embedded variable font are addressable under the family names
    /// its STAT table composes, next to the font's own typographic family.
    /// </summary>
    public class VariableFontInstanceFamilyTests
    {
        private const string CollectionKey = "fonts:instances";

        private const string InterVariableSource =
            "resm:Avalonia.Skia.UnitTests.Assets.InterVariable.ttf?assembly=Avalonia.Skia.UnitTests";

        [Fact]
        public void Display_Family_SemiBold_Resolves_The_Display_SemiBold_Instance()
        {
            using (Start(out var root, out _))
            {
                var glyphTypeface = Resolve(new Typeface(
                    CollectionKey + "#Inter Variable Display", FontStyle.Normal, FontWeight.SemiBold));

                Assert.Equal(FontSimulations.None, glyphTypeface.FontSimulations);
                Assert.Equal(FontWeight.SemiBold, glyphTypeface.Weight);
                Assert.Equal(Position(root, "opsz=32,wght=600"), glyphTypeface.VariationPosition);
            }
        }

        [Fact]
        public void Display_Family_Regular_Sits_At_The_Display_Optical_Size()
        {
            using (Start(out var root, out _))
            {
                var glyphTypeface = Resolve(new Typeface(CollectionKey + "#Inter Variable Display"));

                Assert.Equal(FontSimulations.None, glyphTypeface.FontSimulations);
                Assert.Equal(Position(root, "opsz=32,wght=400"), glyphTypeface.VariationPosition);
            }
        }

        [Fact]
        public void Display_Family_Reaches_Weights_Between_Instances_At_Its_Optical_Size()
        {
            using (Start(out var root, out _))
            {
                var glyphTypeface = Resolve(new Typeface(
                    CollectionKey + "#Inter Variable Display", weight: (FontWeight)450));

                Assert.Equal(FontSimulations.None, glyphTypeface.FontSimulations);
                Assert.Equal(Position(root, "opsz=32,wght=450"), glyphTypeface.VariationPosition);
            }
        }

        [Fact]
        public void Text_Family_Bold_Resolves_The_Text_Bold_Instance()
        {
            using (Start(out var root, out _))
            {
                var glyphTypeface = Resolve(new Typeface(
                    CollectionKey + "#Inter Variable Text", FontStyle.Normal, FontWeight.Bold));

                Assert.Equal(FontSimulations.None, glyphTypeface.FontSimulations);
                Assert.Equal(FontWeight.Bold, glyphTypeface.Weight);
                Assert.Equal(Position(root, "opsz=14,wght=700"), glyphTypeface.VariationPosition);
            }
        }

        [Fact]
        public void Typographic_Family_Regular_Still_Resolves_The_Default_Face()
        {
            using (Start(out var root, out _))
            {
                Resolve(new Typeface(CollectionKey + "#Inter Variable Display", weight: FontWeight.SemiBold));

                var glyphTypeface = Resolve(new Typeface(CollectionKey + "#Inter Variable"));

                Assert.Same(root, glyphTypeface);
                Assert.True(glyphTypeface.VariationPosition.IsDefault);
            }
        }

        [Fact]
        public void Instance_Family_Resolution_Is_Cached()
        {
            using (Start(out _, out _))
            {
                var typeface = new Typeface(CollectionKey + "#Inter Variable Display", weight: FontWeight.SemiBold);

                Assert.Same(Resolve(typeface), Resolve(typeface));
            }
        }

        [Fact]
        public void Unknown_Instance_Family_Does_Not_Resolve()
        {
            using (Start(out _, out var collection))
            {
                Assert.False(collection.TryGetGlyphTypeface("Inter Variable Poster", FontStyle.Normal,
                    FontWeight.Normal, FontStretch.Normal, out _));
            }
        }

        [Fact]
        public void Collection_Lists_The_Instance_Families()
        {
            using (Start(out _, out var collection))
            {
                var names = collection.Select(x => x.Name).ToArray();

                Assert.Contains("Inter Variable", names);
                Assert.Contains("Inter Variable Display", names);
                Assert.Contains("Inter Variable Text", names);

                Assert.True(collection.TryGetFamilyTypefaces("Inter Variable Display", out var typefaces));
                Assert.NotEmpty(typefaces);
            }
        }

        [Fact]
        public void Instance_Families_Are_Created_On_First_Lookup()
        {
            using (Start(out _, out var collection))
            {
                Assert.False(collection._glyphTypefaceCache.ContainsKey("Inter Variable Display"));

                Assert.True(collection.TryGetInstanceFamilyFaces("Inter Variable Display", out var faces));

                var face = Assert.Single(faces);

                Assert.True(collection._glyphTypefaceCache.ContainsKey("Inter Variable Display"));
                Assert.Same(face, collection._glyphTypefaceCache["Inter Variable Display"].Values.Single());
            }
        }

        [Fact]
        public void Instance_Families_Do_Not_Share_Faces()
        {
            using (Start(out var root, out var collection))
            {
                Assert.True(collection.TryGetInstanceFamilyFaces("Inter Variable Display", out var display));
                Assert.True(collection.TryGetInstanceFamilyFaces("Inter Variable Text", out var text));

                Assert.Equal(Position(root, "opsz=32"), Assert.Single(display).VariationPosition);
                Assert.Same(root, Assert.Single(text));
                Assert.DoesNotContain(collection._glyphTypefaceCache["Inter Variable"].Values,
                    x => x is not null && !x.VariationPosition.IsDefault);
            }
        }

        [Fact]
        public void Character_Match_In_Instance_Family_Resolves_Back_To_That_Family()
        {
            using (Start(out var root, out var collection))
            {
                // The typographic family already holds a SemiBold at the default optical size.
                Resolve(new Typeface(CollectionKey + "#Inter Variable", weight: FontWeight.SemiBold));
                Resolve(new Typeface(CollectionKey + "#Inter Variable Display"));

                Assert.True(collection.TryMatchCharacter('A', FontStyle.Normal, FontWeight.SemiBold,
                    FontStretch.Normal, "Inter Variable Display", null, out var match));

                var glyphTypeface = Resolve(match);

                Assert.Equal(FontSimulations.None, glyphTypeface.FontSimulations);
                Assert.Equal(Position(root, "opsz=32,wght=600"), glyphTypeface.VariationPosition);
            }
        }

        private static GlyphTypeface Resolve(Typeface typeface)
        {
            Assert.True(FontManager.Current.TryGetGlyphTypeface(typeface, out var glyphTypeface));

            return glyphTypeface;
        }

        private static NormalizedVariationPosition Position(GlyphTypeface root, string settings)
            => root.CreateNormalizedPosition(FontVariationSettings.Parse(settings));

        private static IDisposable Start(out GlyphTypeface root, out EmbeddedFontCollection collection)
        {
            var app = UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl()));

            collection = new EmbeddedFontCollection(
                new Uri(CollectionKey, UriKind.Absolute),
                new Uri(InterVariableSource, UriKind.Absolute));

            FontManager.Current.AddFontCollection(collection);

            root = collection._glyphTypefaceCache["Inter Variable"].Values.Single()!;

            return app;
        }
    }
}
