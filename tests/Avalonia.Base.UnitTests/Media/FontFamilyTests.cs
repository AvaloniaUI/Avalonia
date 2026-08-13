using System;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class FontFamilyTests
    {
        public FontFamilyTests()
        {
            AssetLoader.RegisterResUriParsers();
        }

        [Fact]
        public void Should_Implicitly_Convert_String_To_FontFamily()
        {
            FontFamily fontFamily = "Arial";

            Assert.Equal(new FontFamily("Arial"), fontFamily);
        }

        [InlineData("Font A")]
        [InlineData("Font A, Font B")]
        [InlineData("resm: Avalonia.Visuals.UnitTests#MyFont")]
        [InlineData("avares://Avalonia.Visuals.UnitTests/Assets/Fonts#MyFont")]
        [Theory]
        public void Should_Have_Equal_Hash(string s)
        {
            var fontFamily = new FontFamily(s);

            Assert.Equal(new FontFamily(s).GetHashCode(), fontFamily.GetHashCode());
        }

        [InlineData("Font A, Font B", "Font B, Font A")]
        [InlineData("Font A, Font B", "Font A, Font C")]
        [Theory]
        public void Should_Not_Have_Equal_Hash(string a, string b)
        {
            var fontFamily = new FontFamily(b);

            Assert.NotEqual(new FontFamily(a).GetHashCode(), fontFamily.GetHashCode());
        }

        [InlineData("Font A")]
        [InlineData("Font A, Font B")]
        [InlineData("resm: Avalonia.Visuals.UnitTests#MyFont")]
        [InlineData("avares://Avalonia.Visuals.UnitTests/Assets/Fonts#MyFont")]
        [Theory]
        public void Should_Be_Equal(string s)
        {
            var fontFamily = new FontFamily(s);

            Assert.Equal(new FontFamily(s), fontFamily);
        }

        [InlineData("Font A, Font B", "Font B, Font A")]
        [InlineData("Font A, Font B", "Font A, Font C")]
        [Theory]
        public void Should_Not_Be_Equal(string a, string b)
        {
            var fontFamily = new FontFamily(b);

            Assert.NotEqual(new FontFamily(a), fontFamily);
        }

        [Fact]
        public void Should_Parse_FontFamily_With_SystemFont_Name()
        {
            var fontFamily = FontFamily.Parse("Courier New");

            Assert.Equal("Courier New", fontFamily.Name);
        }

        [Fact]
        public void Should_Parse_FontFamily_With_Fallbacks()
        {
            var fontFamily = FontFamily.Parse("Courier New, Times New Roman");

            Assert.Equal("Courier New", fontFamily.Name);

            Assert.Equal(2, fontFamily.FamilyNames.Count);

            Assert.Equal("Times New Roman", fontFamily.FamilyNames.Last());
        }

        [Fact]
        public void Should_Parse_FontFamily_With_Resource_Folder()
        {
            var source = new Uri("resm:Avalonia.Visuals.UnitTests#MyFont");

            var key = new FontFamilyKey(source);

            var fontFamily = FontFamily.Parse(source.OriginalString);

            Assert.Equal("MyFont", fontFamily.Name);

            Assert.Equal(key, fontFamily.Key);
        }

        [Fact]
        public void Should_Parse_FontFamily_With_Resource_Filename()
        {
            var source = new Uri("resm:Avalonia.Visuals.UnitTests.MyFont.ttf#MyFont");

            var key = new FontFamilyKey(source);

            var fontFamily = FontFamily.Parse(source.OriginalString);

            Assert.Equal("MyFont", fontFamily.Name);

            Assert.Equal(key, fontFamily.Key);
        }

        [Theory]
        [InlineData("resm:Avalonia.Visuals.UnitTests/Assets/Fonts#MyFont")]
        [InlineData("avares://Avalonia.Visuals.UnitTests/Assets/Fonts#MyFont")]
        public void Should_Create_FontFamily_From_Uri(string name)
        {
            var fontFamily = new FontFamily(name);

            Assert.Equal("MyFont", fontFamily.Name);

            Assert.NotNull(fontFamily.Key);
        }

        [Theory]
        [InlineData(null, "resm:Avalonia.Visuals.UnitTests.Assets.Fonts#MyFont")]
        [InlineData("avares://Avalonia.Visuals.UnitTests/Assets/Fonts", "/#MyFont")]
        [InlineData("avares://Avalonia.Visuals.UnitTests", "/Assets/Fonts#MyFont")]
        public void Should_Create_FontFamily_From_Uri_With_Base_Uri(string? @base, string name)
        {
            var baseUri = @base != null ? new Uri(@base) : null;

            var fontFamily = new FontFamily(baseUri, name);

            Assert.Equal("MyFont", fontFamily.Name);

            Assert.NotNull(fontFamily.Key);
        }

        [InlineData(null, "Arial", "Arial", null)]
        [InlineData(null, "resm:Avalonia.Skia.UnitTests.Fonts?assembly=Avalonia.Skia.UnitTests#Manrope", "Manrope", "resm:Avalonia.Skia.UnitTests.Fonts?assembly=Avalonia.Skia.UnitTests")]
        [InlineData(null, "avares://Avalonia.Fonts.Inter/Assets#Inter", "Inter", null)]
        [InlineData("avares://Avalonia.Fonts.Inter", "/Assets#Inter", "Inter", "avares://Avalonia.Fonts.Inter/Assets")]
        [InlineData("avares://ControlCatalog/MainWindow.xaml", "avares://Avalonia.Fonts.Inter/Assets#Inter", "Inter", "avares://Avalonia.Fonts.Inter/Assets")]
        [Theory]
        public void Should_Parse_FontFamily_With_BaseUri(string? baseUri, string s, string expectedName, string? expectedUri)
        {
            var b = baseUri is not null ? new Uri(baseUri) : null;

            expectedUri = expectedUri is not null ? new Uri(expectedUri).AbsoluteUri : null;

            var fontFamily = FontFamily.Parse(s, b);

            Assert.Equal(expectedName, fontFamily.Name);

            var key = fontFamily.Key;

            if (expectedUri is not null)
            {
                Assert.NotNull(key);

                if (key.BaseUri is not null)
                {
                    Assert.True(key.BaseUri.IsAbsoluteUri);
                }

                if (key.BaseUri is null)
                {
                    Assert.NotNull(key.Source);
                    Assert.True(key.Source.IsAbsoluteUri);
                }

                var fontUri = key.BaseUri;

                if (key.Source is Uri sourceUri)
                {
                    if (sourceUri.IsAbsoluteUri)
                    {
                        fontUri = sourceUri;
                    }
                    else
                    {
                        Assert.NotNull(fontUri);
                        fontUri = new Uri(fontUri, sourceUri);
                    }
                }

                Assert.NotNull(fontUri);
                Assert.Equal(expectedUri, fontUri.AbsoluteUri);
            }
        }

        [InlineData("avares://MyAssembly/", "Some/Path/#FontName", "avares://MyAssembly/Some/Path/"), ]
        [InlineData("avares://MyAssembly/", "./Some/Path/#FontName", "avares://MyAssembly/Some/Path/")]
        [InlineData("avares://MyAssembly/sub/", "../Some/Path/#FontName", "avares://MyAssembly/Some/Path/")]
        [Theory]
        public void Should_Parse_Relative_Path(string baseUriString, string path, string expected)
        {
            var baseUri = new Uri(baseUriString, UriKind.Absolute);

            var fontFamily = FontFamily.Parse(path, baseUri);

            Assert.NotNull(fontFamily.Key);

            Assert.NotNull(fontFamily.Key.BaseUri);

            Assert.NotNull(fontFamily.Key.Source);

            var actual = new Uri(fontFamily.Key.BaseUri, fontFamily.Key.Source);

            Assert.Equal(expected, actual.AbsoluteUri);
        }
    }
}
