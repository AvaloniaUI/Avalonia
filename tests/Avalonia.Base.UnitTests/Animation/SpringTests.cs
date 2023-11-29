using System;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Styling;
using Xunit;

namespace Avalonia.Base.UnitTests.Animation;

public class SpringTests
{
    [Theory]
    [InlineData("1,2 3,4")]
    public void Can_Parse_Spring_Via_TypeConverter(string input)
    {
        var conv = new SpringTypeConverter();

        var spring = (Spring)conv.ConvertFrom(input);

        Assert.NotNull(spring);
        Assert.Equal(1, spring.Mass);
        Assert.Equal(2, spring.Stiffness);
        Assert.Equal(3, spring.Damping);
        Assert.Equal(4, spring.InitialVelocity);
    }

    [Theory]
    [InlineData("1,2F,3,4")]
    [InlineData("Foo,Bar,Fee,Buzz")]
    public void Can_Handle_Invalid_String_Via_TypeConverter(string input)
    {
        var conv = new SpringTypeConverter();

        Assert.ThrowsAny<Exception>(() => (Spring)conv.ConvertFrom(input));
    }

    [Fact]
    public void SplineEasing_Can_Be_Mutated()
    {
        var easing = new SpringEasing(1, 1, 1);

        Assert.Equal(0, easing.Ease(0));
        Assert.Equal(0.34029984660829826, easing.Ease(1));

        easing.Mass = 2;
        easing.Stiffness = 2;
        easing.Damping = 2;
        easing.InitialVelocity = 1;

        Assert.NotEqual(0.05136985716812037, easing.Ease(0.5));
    }

    [Fact]
    public void Check_SpringEasing_Handled_properly()
    {
        var keyframe1 = new KeyFrame()
        {
            Setters = { new Setter(RotateTransform.AngleProperty, -2.5d), }, KeyTime = TimeSpan.FromSeconds(0)
        };

        var keyframe2 = new KeyFrame()
        {
            Setters = { new Setter(RotateTransform.AngleProperty, 2.5d), }, KeyTime = TimeSpan.FromSeconds(5)
        };

        var animation = new Avalonia.Animation.Animation()
        {
            Duration = TimeSpan.FromSeconds(5),
            Children = { keyframe1, keyframe2 },
            IterationCount = new IterationCount(5),
            PlaybackDirection = PlaybackDirection.Alternate,
            Easing = new SpringEasing(1, 10, 1)
        };

        var rotateTransform = new RotateTransform(-2.5);
        var rect = new Rectangle() { RenderTransform = rotateTransform };

        var clock = new TestClock();
        animation.RunAsync(rect, clock);

        clock.Step(TimeSpan.Zero);
        Assert.Equal(-2.5, rotateTransform.Angle);
        clock.Step(TimeSpan.FromSeconds(5));
        Assert.Equal(5.522828945000075, rotateTransform.Angle);

        var tolerance = 0.01;
        clock.Step(TimeSpan.Parse("00:00:10.0153932"));
        var expected = -2.499763294237805;
        Assert.True(Math.Abs(rotateTransform.Angle - expected) <= tolerance);

        clock.Step(TimeSpan.Parse("00:00:11.2655407"));
        expected = -1.1011448950348934;
        Assert.True(Math.Abs(rotateTransform.Angle - expected) <= tolerance);

        clock.Step(TimeSpan.Parse("00:00:12.6158773"));
        expected = 2.1264981706749007;
        Assert.True(Math.Abs(rotateTransform.Angle - expected) <= tolerance);

        clock.Step(TimeSpan.Parse("00:00:14.6495256"));
        expected = 5.4337608446234782;
        Assert.True(Math.Abs(rotateTransform.Angle - expected) <= tolerance);
    }
}
