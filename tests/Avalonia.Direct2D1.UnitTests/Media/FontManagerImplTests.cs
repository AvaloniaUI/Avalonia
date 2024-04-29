using Avalonia.Direct2D1.Media;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Direct2D1.UnitTests.Media
{
    public class FontManagerImplTests
    {
        private static string s_fontUri = "resm:Avalonia.Direct2D1.UnitTests.Assets?assembly=Avalonia.Direct2D1.UnitTests#Noto Mono";

        [Fact]
        public void Should_Create_Typeface_From_Fallback()
        {
            using (AvaloniaLocator.EnterScope())
            {
                Direct2D1Platform.Initialize();

                var typeface = new Typeface(new FontFamily("A, B, Arial"));

                var glyphTypeface = typeface.GlyphTypeface;

                Assert.Equal("Arial", glyphTypeface.FamilyName);
            }
        }

        [Fact]
        public void Should_Create_Typeface_From_Fallback_Bold()
        {
            using (AvaloniaLocator.EnterScope())
            {
                Direct2D1Platform.Initialize();

                var typeface = new Typeface(new FontFamily("A, B, Arial"), weight: FontWeight.Bold);

                var glyphTypeface = typeface.GlyphTypeface;

                Assert.Equal("Arial", glyphTypeface.FamilyName);

                Assert.Equal(FontWeight.Bold, glyphTypeface.Weight);

                Assert.Equal(FontStyle.Normal, glyphTypeface.Style);
            }
        }

        [Fact]
        public void Should_Create_Typeface_For_Unknown_Font()
        {
            using (AvaloniaLocator.EnterScope())
            {
                Direct2D1Platform.Initialize();

                var glyphTypeface = new Typeface(new FontFamily("Unknown")).GlyphTypeface;

                var defaultName = FontManager.Current.DefaultFontFamily.Name;

                Assert.Equal(defaultName, glyphTypeface.FamilyName);
            }
        }

        [Fact]
        public void Should_Load_Typeface_From_Resource()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                Direct2D1Platform.Initialize();

                var fontManager = new FontManagerImpl();

                var glyphTypeface = new Typeface(s_fontUri).GlyphTypeface;

                Assert.Equal("Noto Mono", glyphTypeface.FamilyName);
            }
        }

        [Fact]
        public void Should_Load_Nearest_Matching_Font()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                Direct2D1Platform.Initialize();

                var glyphTypeface = new Typeface(s_fontUri, FontStyle.Italic, FontWeight.Black).GlyphTypeface;

                Assert.Equal("Noto Mono", glyphTypeface.FamilyName);
            }
        }
    }
}
