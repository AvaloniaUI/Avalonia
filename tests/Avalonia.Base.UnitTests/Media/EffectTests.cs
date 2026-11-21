using System;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media;

public class EffectTests : ScopedTestBase
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

    // Direction is measured anticlockwise with Y pointing up, as in WPF.
    [Theory,
     InlineData(0, 10, 0),
     InlineData(90, 0, -10),
     InlineData(180, -10, 0),
     InlineData(270, 0, 10),
     InlineData(315, 7.0711, 7.0711)
    ]
    public void DropShadowDirectionEffect_Offset_Follows_Direction(double direction, double x, double y)
    {
        var effect = new DropShadowDirectionEffect { Direction = direction, ShadowDepth = 10 };
        Assert.Equal(x, effect.OffsetX, 4);
        Assert.Equal(y, effect.OffsetY, 4);
    }

    [Theory,
     InlineData(0, 10, 0),
     InlineData(90, 0, -10),
     InlineData(180, -10, 0),
     InlineData(270, 0, 10),
     InlineData(315, 7.0711, 7.0711)
    ]
    public void ImmutableDropShadowDirectionEffect_Offset_Follows_Direction(double direction, double x, double y)
    {
        var effect = new ImmutableDropShadowDirectionEffect(direction, 10, 0, Colors.Black, 1);
        Assert.Equal(x, effect.OffsetX, 4);
        Assert.Equal(y, effect.OffsetY, 4);
    }

    [Fact]
    public void DropShadowDirectionEffect_ToImmutable_Preserves_Direction_And_ShadowDepth()
    {
        var effect = new DropShadowDirectionEffect
        {
            Direction = 270,
            ShadowDepth = 10,
            BlurRadius = 3,
            Color = Colors.Red,
            Opacity = 0.5
        };

        var immutable = (ImmutableDropShadowDirectionEffect)effect.ToImmutable();

        Assert.Equal(270, immutable.Direction);
        Assert.Equal(10, immutable.ShadowDepth);
        Assert.Equal(3, immutable.BlurRadius);
        Assert.Equal(Colors.Red, immutable.Color);
        Assert.Equal(0.5, immutable.Opacity);
        Assert.Equal(effect.OffsetX, immutable.OffsetX);
        Assert.Equal(effect.OffsetY, immutable.OffsetY);
    }
}

public class BackdropEffectCacheTests
{
    [Fact]
    public void Parse_Parses_Retained()
        => Assert.IsType<RetainedBackdropEffectCache>(BackdropEffectCache.Parse("Retained"));

    [Fact]
    public void Parse_Parses_Volatile()
        => Assert.IsType<VolatileBackdropEffectCache>(BackdropEffectCache.Parse("Volatile"));

    [Fact]
    public void Parse_Default_Returns_Null()
        => Assert.Null(BackdropEffectCache.Parse("Default"));

    [Fact]
    public void Invalid_Parse_Fails()
        => Assert.Throws<ArgumentException>(() => BackdropEffectCache.Parse("Nope"));
}
