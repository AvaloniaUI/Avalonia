using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.Data;
using Xunit;

namespace Avalonia.Animation.UnitTests
{
    public class AnimationIterationTests
    {
        [Fact]
        public void Check_Initial_Inter_and_Trailing_Delay_Values()
        {
            var keyframe1 = new KeyFrame()
            {
                Setters =
                {
                    new Setter(Border.WidthProperty, 200d),
                },
                Cue = new Cue(1d)
            };

            var keyframe2 = new KeyFrame()
            {
                Setters =
                {
                    new Setter(Border.WidthProperty, 100d),
                },
                Cue = new Cue(0d)
            };

            var animation = new Animation()
            {
                Duration = TimeSpan.FromSeconds(3),
                Delay = TimeSpan.FromSeconds(3),
                DelayBetweenIterations = TimeSpan.FromSeconds(3),
                IterationCount = new IterationCount(2),
                Children =
                {
                    keyframe2,
                    keyframe1
                }
            };

            var border = new Border()
            {
                Height = 100d,
                Width = 100d
            };

            var clock = new TestClock();
            var animationRun = animation.RunAsync(border, clock);

            clock.Step(TimeSpan.Zero);

            // Initial Delay.
            clock.Step(TimeSpan.FromSeconds(1));
            Assert.Equal(border.Width, 0d);

            clock.Step(TimeSpan.FromSeconds(6));

            // First Inter-Iteration delay.
            clock.Step(TimeSpan.FromSeconds(8));
            Assert.Equal(border.Width, 200d);

            // Trailing Delay should be non-existent.
            clock.Step(TimeSpan.FromSeconds(14));
            Assert.True(animationRun.Status == TaskStatus.RanToCompletion);
            Assert.Equal(border.Width, 100d);
        }
    }
}
