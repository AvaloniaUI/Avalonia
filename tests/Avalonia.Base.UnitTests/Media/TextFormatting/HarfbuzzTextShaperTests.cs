using System;
using System.Linq;
using Avalonia.Base.UnitTests.Media.Fonts.Tables;
using Avalonia.Harfbuzz;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.TextFormatting;

public class HarfBuzzTextShaperTests
{
    private static readonly string s_InterFontUri =
        "resm:Avalonia.Base.UnitTests.Assets.Inter-Regular.ttf?assembly=Avalonia.Base.UnitTests";

    private readonly HarfBuzzTextShaper _shaper;
    
    private TestServices Services => TestServices.MockThreadingInterface.With(
        textShaperImpl: _shaper);

    public HarfBuzzTextShaperTests()
    {
        _shaper = new HarfBuzzTextShaper();
    }

    [Fact]
    public void ShapeText_WithValidInput_ReturnsShapedBuffer()
    {
        using (UnitTestApplication.Start(Services))
        {
            var text = "Hello World".AsMemory();
            var options = CreateTextShaperOptions();
            
            var result = _shaper.ShapeText(text, options);
            
            Assert.NotNull(result);
            Assert.Equal(text.Length, result.Length);
        }
    }

    [Fact]
    public void ShapeText_WithEmptyString_ReturnsEmptyShapedBuffer()
    {
        using (UnitTestApplication.Start(Services))
        {
            var text = "".AsMemory();
            var options = CreateTextShaperOptions();
            
            var result = _shaper.ShapeText(text, options);
            
            Assert.NotNull(result);
            Assert.Equal(0, result.Length);
        }
    }

    [Fact]
    public void ShapeText_WithTabCharacter_ReplacesWithSpace()
    {
        using (UnitTestApplication.Start(Services))
        {
            var text = "Hello\tWorld".AsMemory();
            var options = CreateTextShaperOptions();
            
            var result = _shaper.ShapeText(text, options);
            
            Assert.NotNull(result);
            Assert.True(result.Length == 11);
        }
    }

    [Fact]
    public void ShapeText_WithCRLF_MergesBreakPair()
    {
        using (UnitTestApplication.Start(Services))
        {
            var text = "Line1\r\nLine2".AsMemory();
            var options = CreateTextShaperOptions();
            
            var result = _shaper.ShapeText(text, options);
            
            Assert.NotNull(result);
            Assert.NotEqual(0.0, result[5].GlyphAdvance);
        }
    }
    
    [Fact]
    public void ShapeText_EndWithCRLF_MergesBreakPair()
    {
        using (UnitTestApplication.Start(Services))
        {
            var text = "Line1\r\n".AsMemory();
            var options = CreateTextShaperOptions();
            
            var result = _shaper.ShapeText(text, options);
            
            Assert.NotNull(result);
            Assert.Equal(0.0, result[5].GlyphAdvance);
        }
    }

    private TextShaperOptions CreateTextShaperOptions(
        sbyte bidiLevel = 0,
        double letterSpacing = 0,
        double fontSize = 16)
    {
        var assetLoader = new StandardAssetLoader();

        using var stream = assetLoader.Open(new Uri(s_InterFontUri));

        var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

        return new TextShaperOptions(
            typeface,
            fontSize,
            bidiLevel,
            letterSpacing: letterSpacing);
    }
}
