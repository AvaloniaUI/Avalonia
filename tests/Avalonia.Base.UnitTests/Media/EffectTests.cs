using System;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Base.UnitTests.Media;

public class EffectTests
{
    [Fact]
    public void Parse_Parses_Blur()
    {
        var effect = (ImmutableBlurEffect)Effect.Parse("blur(123.34)");
        Assert.Equal(123.34, effect.Radius);
    }

    private const uint Black = 0xff000000;

    [Theory,
     InlineData("drop-shadow(10 20)", 10, 20, 0, Black),
     InlineData("drop-shadow( 10  20 ) ", 10, 20, 0, Black),
     InlineData("drop-shadow( 10  20 30 ) ", 10, 20, 30, Black),
     InlineData("drop-shadow(10  20 30)", 10, 20, 30, Black),
     InlineData("drop-shadow(-10  -20 30)", -10, -20, 30, Black),
     InlineData("drop-shadow(10 20 30 #ffff00ff)", 10, 20, 30, 0xffff00ff),
     InlineData("drop-shadow ( 10 20 30 #ffff00ff ) ", 10, 20, 30, 0xffff00ff),
     InlineData("drop-shadow(10 20 30 red)", 10, 20, 30, 0xffff0000),
     InlineData("drop-shadow ( 10   20   30 red  ) ", 10, 20, 30, 0xffff0000),
     InlineData("drop-shadow(10 20 30 rgba(100, 30, 45, 90%))", 10, 20, 30, 0xe6641e2d),
     InlineData("drop-shadow(10 20 30  rgba(100, 30, 45, 90%) ) ", 10, 20, 30, 0xe6641e2d)
    ]
    public void Parse_Parses_DropShadow(string s, double x, double y, double r, uint color)
    {
        var effect = (ImmutableDropShadowEffect)Effect.Parse(s);
        Assert.Equal(x, effect.OffsetX);
        Assert.Equal(y, effect.OffsetY);
        Assert.Equal(r, effect.BlurRadius);
        Assert.Equal(1, effect.Opacity);
        Assert.Equal(color, effect.Color.ToUInt32());
    }

    [Theory,
     InlineData("blur"),
     InlineData("blur("),
     InlineData("blur()"),
     InlineData("blur(123"),
     InlineData("blur(aaab)"),
     InlineData("drop-shadow(-10  -20 -30)")
    ]
    public void Invalid_Effect_Parse_Fails(string b)
    {
        Assert.Throws<ArgumentException>(() => Effect.Parse(b));
    }

    [Theory,
     InlineData("blur(2.5)", 4, 4, 4, 4),
     InlineData("blur(0)", 0, 0, 0, 0),
     InlineData("drop-shadow(10 15)", 0, 0, 10, 15),
     InlineData("drop-shadow(10 15 5)", 0, 0, 16, 21),
     InlineData("drop-shadow(0 0 5)", 6, 6, 6, 6),
     InlineData("drop-shadow(3 3 5)", 3, 3, 9, 9)
    ]
    public static void PaddingIsCorrectlyCalculated(string effect, double left, double top, double right, double bottom)
    {
        var padding = Effect.Parse(effect).GetEffectOutputPadding();
        Assert.Equal(left, padding.Left);
        Assert.Equal(top, padding.Top);
        Assert.Equal(right, padding.Right);
        Assert.Equal(bottom, padding.Bottom);
    }
}
