using System;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media
{
    public class FontFamilyTests
    {
        [InlineData(null, "Arial", "Arial", null)]
        [InlineData(null, "resm:Avalonia.Skia.UnitTests.Fonts?assembly=Avalonia.Skia.UnitTests#Manrope", "Manrope", "resm:Avalonia.Skia.UnitTests.Fonts?assembly=Avalonia.Skia.UnitTests")]
        [InlineData(null, "avares://Avalonia.Fonts.Inter/Assets#Inter", "Inter", "avares://Avalonia.Fonts.Inter/Assets")]
        [InlineData("avares://Avalonia.Fonts.Inter", "/Assets#Inter", "Inter", "avares://Avalonia.Fonts.Inter/Assets")]
        [Theory]
        public void Should_Parse_FontFamily_With_BaseUri(string baseUri, string s, string expectedName, string expectedUri)
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

                Uri fontUri = key.BaseUri;

                if (key.Source is Uri sourceUri)
                {
                    if (sourceUri.IsAbsoluteUri)
                    {
                        fontUri = sourceUri;
                    }
                    else
                    {
                        fontUri = new Uri(fontUri, sourceUri);
                    }
                }

                Assert.Equal(expectedUri, fontUri.AbsoluteUri);
            }
        }
    }
}
