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

                var fontManager = new FontManagerImpl();

                var glyphTypeface = (GlyphTypefaceImpl)fontManager.CreateGlyphTypeface(
                    new Typeface(new FontFamily("A, B, Arial")));

                var font = glyphTypeface.DWFont;

                Assert.Equal("Arial", font.FontFamily.FamilyNames.GetString(0));

                Assert.Equal(SharpDX.DirectWrite.FontWeight.Normal, font.Weight);

                Assert.Equal(SharpDX.DirectWrite.FontStyle.Normal, font.Style);
            }
        }

        [Fact]
        public void Should_Create_Typeface_From_Fallback_Bold()
        {
            using (AvaloniaLocator.EnterScope())
            {
                Direct2D1Platform.Initialize();

                var fontManager = new FontManagerImpl();

                var glyphTypeface = (GlyphTypefaceImpl)fontManager.CreateGlyphTypeface(
                    new Typeface(new FontFamily("A, B, Arial"), weight: FontWeight.Bold));

                var font = glyphTypeface.DWFont;

                Assert.Equal("Arial", font.FontFamily.FamilyNames.GetString(0));

                Assert.Equal(SharpDX.DirectWrite.FontWeight.Bold, font.Weight);

                Assert.Equal(SharpDX.DirectWrite.FontStyle.Normal, font.Style);
            }
        }

        [Fact]
        public void Should_Create_Typeface_For_Unknown_Font()
        {
            using (AvaloniaLocator.EnterScope())
            {
                Direct2D1Platform.Initialize();

                var fontManager = new FontManagerImpl();

                var glyphTypeface = (GlyphTypefaceImpl)fontManager.CreateGlyphTypeface(
                    new Typeface(new FontFamily("Unknown")));

                var font = glyphTypeface.DWFont;

                var defaultName = fontManager.GetDefaultFontFamilyName();

                Assert.Equal(defaultName, font.FontFamily.FamilyNames.GetString(0));

                Assert.Equal(SharpDX.DirectWrite.FontWeight.Normal, font.Weight);

                Assert.Equal(SharpDX.DirectWrite.FontStyle.Normal, font.Style);
            }
        }

        [Fact]
        public void Should_Load_Typeface_From_Resource()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                Direct2D1Platform.Initialize();

                var fontManager = new FontManagerImpl();

                var glyphTypeface = (GlyphTypefaceImpl)fontManager.CreateGlyphTypeface(
                    new Typeface(s_fontUri));

                var font = glyphTypeface.DWFont;

                Assert.Equal("Noto Mono", font.FontFamily.FamilyNames.GetString(0));
            }
        }

        [Fact]
        public void Should_Load_Nearest_Matching_Font()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                Direct2D1Platform.Initialize();

                var fontManager = new FontManagerImpl();

                var glyphTypeface = (GlyphTypefaceImpl)fontManager.CreateGlyphTypeface(
                    new Typeface(s_fontUri, FontStyle.Italic, FontWeight.Black));

                var font = glyphTypeface.DWFont;

                Assert.Equal("Noto Mono", font.FontFamily.FamilyNames.GetString(0));
            }
        }
    }
}
