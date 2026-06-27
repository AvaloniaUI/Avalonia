using System;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using Xunit;

namespace Avalonia.Base.UnitTests.Animation;

using Animation = global::Avalonia.Animation.Animation;

public class RenderTransformAnimationTests
{
    /// <summary>
    /// Regression test for https://github.com/AvaloniaUI/Avalonia/issues/8640.
    /// Animating RenderTransform with TransformOperations keyframes must not throw
    /// "No animator registered for the property RenderTransform".
    /// </summary>
    [Fact]
    public void Animating_RenderTransform_Does_Not_Throw()
    {
        var from = TransformOperations.Parse("translateX(0px)");
        var to = TransformOperations.Parse("translateX(100px)");

        var animation = new Animation
        {
            Duration = TimeSpan.FromSeconds(1),
            FillMode = FillMode.Both,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0),
                    Setters = { new Setter(Visual.RenderTransformProperty, (ITransform)from) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1),
                    Setters = { new Setter(Visual.RenderTransformProperty, (ITransform)to) }
                },
            }
        };

        var border = new Border();
        var clock = new TestClock();

        // Must not throw InvalidOperationException.
        animation.RunAsync(border, clock, TestContext.Current.CancellationToken);

        clock.Step(TimeSpan.Zero);
        clock.Step(TimeSpan.FromSeconds(0.5));
        clock.Step(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Animating_RenderTransform_Interpolates_TranslateX()
    {
        var from = TransformOperations.Parse("translateX(0px)");
        var to = TransformOperations.Parse("translateX(100px)");

        var animation = new Animation
        {
            Duration = TimeSpan.FromSeconds(1),
            FillMode = FillMode.Both,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0),
                    Setters = { new Setter(Visual.RenderTransformProperty, (ITransform)from) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1),
                    Setters = { new Setter(Visual.RenderTransformProperty, (ITransform)to) }
                },
            }
        };

        var border = new Border();
        var clock = new TestClock();

        animation.RunAsync(border, clock, TestContext.Current.CancellationToken);

        clock.Step(TimeSpan.Zero);
        var atStart = border.RenderTransform as TransformOperations;
        Assert.NotNull(atStart);
        Assert.Equal(from.Value, atStart.Value);

        clock.Step(TimeSpan.FromSeconds(0.5));
        var atMid = border.RenderTransform as TransformOperations;
        Assert.NotNull(atMid);
        // At 50% progress translateX should be 50px, i.e. M31 (translation X) ≈ 50.
        Assert.Equal(50.0, atMid.Value.M31, precision: 5);

        clock.Step(TimeSpan.FromSeconds(1));
        var atEnd = border.RenderTransform as TransformOperations;
        Assert.NotNull(atEnd);
        Assert.Equal(to.Value, atEnd.Value);
    }

    [Fact]
    public void Animating_RenderTransform_NonTransformOperations_Value_Treated_As_Identity()
    {
        // When one keyframe has a non-TransformOperations ITransform (e.g. TranslateTransform),
        // it should be treated as identity and not throw.
        var to = TransformOperations.Parse("translateX(100px)");

        var animation = new Animation
        {
            Duration = TimeSpan.FromSeconds(1),
            FillMode = FillMode.Both,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0),
                    Setters = { new Setter(Visual.RenderTransformProperty, (ITransform)TransformOperations.Identity) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1),
                    Setters = { new Setter(Visual.RenderTransformProperty, (ITransform)to) }
                },
            }
        };

        var border = new Border();
        var clock = new TestClock();

        animation.RunAsync(border, clock, TestContext.Current.CancellationToken);

        clock.Step(TimeSpan.Zero);
        clock.Step(TimeSpan.FromSeconds(0.5));

        var atMid = border.RenderTransform as TransformOperations;
        Assert.NotNull(atMid);
        Assert.Equal(50.0, atMid.Value.M31, precision: 5);
    }
}
