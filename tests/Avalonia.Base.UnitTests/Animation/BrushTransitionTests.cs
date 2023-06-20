using System;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Base.UnitTests.Animation
{
    public class BrushTransitionTests
    {
        [Fact]
        public void SolidColorBrush_Opacity_IsInteroplated()
        {
            Test(0, new SolidColorBrush { Opacity = 0 }, new SolidColorBrush { Opacity = 0 });
            Test(0, new SolidColorBrush { Opacity = 0 }, new SolidColorBrush { Opacity = 1 });
            Test(0.5, new SolidColorBrush { Opacity = 0 }, new SolidColorBrush { Opacity = 1 });
            Test(0.5, new SolidColorBrush { Opacity = 0.5 }, new SolidColorBrush { Opacity = 0.5 });
            Test(1, new SolidColorBrush { Opacity = 1 }, new SolidColorBrush { Opacity = 1 });
            // TODO: investigate why this case fails.
            //Test2(1, new SolidColorBrush { Opacity = 0 }, new SolidColorBrush { Opacity = 1 });
        }

        [Fact]
        public void LinearGradientBrush_Opacity_IsInteroplated()
        {
            Test(0, new LinearGradientBrush { Opacity = 0 }, new LinearGradientBrush { Opacity = 0 });
            Test(0, new LinearGradientBrush { Opacity = 0 }, new LinearGradientBrush { Opacity = 1 });
            Test(0.5, new LinearGradientBrush { Opacity = 0 }, new LinearGradientBrush { Opacity = 1 });
            Test(0.5, new LinearGradientBrush { Opacity = 0.5 }, new LinearGradientBrush { Opacity = 0.5 });
            Test(1, new LinearGradientBrush { Opacity = 1 }, new LinearGradientBrush { Opacity = 1 });
        }

        private static void Test(double progress, IBrush oldBrush, IBrush newBrush)
        {
            var clock = new TestClock();
            var border = new Border() { Background = oldBrush };
            BrushTransition sut = new BrushTransition
            {
                Duration = TimeSpan.FromSeconds(1), Property = Border.BackgroundProperty
            };

            sut.Apply(border, clock, oldBrush, newBrush);
            clock.Pulse(TimeSpan.Zero);
            clock.Pulse(sut.Duration * progress);

            Assert.NotNull(border.Background);
            Assert.Equal(oldBrush.Opacity + (newBrush.Opacity - oldBrush.Opacity) * progress,
                border.Background.Opacity);
        }
    }
}
