using System;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Animation;
using Animation = global::Avalonia.Animation.Animation;

public class StyleAnimationTests
{
    [Fact]
    public void Application_ControlTheme_Applies_Animation()
    {
        using var app = new AnimationTestApplication
        {
            Resources =
            {
                { typeof(Button), CreateControlTheme<Button>(CreateOpacityAnimation()) },
            },
        };

        var target = new Button();
        var root = CreateRoot(target, app);

        app.Clock.Pulse(TimeSpan.Zero);
        Assert.Equal(1, target.Opacity);
        
        app.Clock.Pulse(TimeSpan.FromSeconds(0.5));
        Assert.Equal(0.5, target.Opacity);

        app.Clock.Pulse(TimeSpan.FromSeconds(1));
        Assert.Equal(0, target.Opacity);
    }

    [Fact]
    public void Application_ControlTheme_Applies_Animation_With_Bound_KeyFrame_Values()
    {
        var fromBinding = new Binding("From");
        var toBinding = new Binding("To");

        using var app = new AnimationTestApplication
        {
            Resources =
            {
                { typeof(Button), CreateControlTheme<Button>(CreateOpacityAnimation(fromBinding, toBinding)) },
            },
        };

        var target = new Button { DataContext = new KeyFrameValues(1.0, 0.0) };
        var root = CreateRoot(target, app);

        app.Clock.Pulse(TimeSpan.Zero);
        Assert.Equal(1, target.Opacity);

        app.Clock.Pulse(TimeSpan.FromSeconds(0.5));
        Assert.Equal(0.5, target.Opacity);

        app.Clock.Pulse(TimeSpan.FromSeconds(1));
        Assert.Equal(0, target.Opacity);
    }

    private static ControlTheme CreateControlTheme<T>(Animation animation)
    {
        return new ControlTheme(typeof(T))
        {
            Animations = { animation }
        };
    }

    private static object CreateRoot(Button child, Application app)
    {
        var root = new TestRoot{ StylingParent = app };
        root.Child = child;
        root.LayoutManager.ExecuteInitialLayoutPass();
        return root;
    }

    private static Animation CreateOpacityAnimation()
    {
        return CreateOpacityAnimation(1.0, 0.0);
    }

    private static Animation CreateOpacityAnimation(object from, object to)
    {
        return new Animation
        {
            Duration = TimeSpan.FromSeconds(1),
            FillMode = FillMode.Both,
            Children =
            {
                new KeyFrame
                {
                    KeyTime = TimeSpan.FromSeconds(0),
                    Setters = { new Setter(Button.OpacityProperty, from) }
                },
                new KeyFrame
                {
                    KeyTime = TimeSpan.FromSeconds(1),
                    Setters = {new Setter(Button.OpacityProperty, to) }
                },
            }
        };
    }

    private class AnimationTestApplication : Application, IDisposable
    {
        private readonly IDisposable _lifetime;

        public AnimationTestApplication()
        {
            var services = new TestServices(globalClock: Clock);
            _lifetime = UnitTestApplication.Start(services);
        }

        public MockGlobalClock Clock { get; } = new MockGlobalClock();

        public void Dispose() => _lifetime.Dispose();
    }

    private record KeyFrameValues(double From, double To);
}
