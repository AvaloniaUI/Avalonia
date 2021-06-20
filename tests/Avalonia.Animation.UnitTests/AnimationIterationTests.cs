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
using Avalonia.Animation.Easings;
using System.Threading;
using System.Reactive.Linq;

namespace Avalonia.Animation.UnitTests
{
    public class AnimationIterationTests
    {
        [Fact]
        public void Check_KeyTime_Correctly_Converted_To_Cue()
        {
            var keyframe1 = new KeyFrame()
            {
                Setters =
                {
                    new Setter(Border.WidthProperty, 100d),
                },
                KeyTime = TimeSpan.FromSeconds(0.5)
            };

            var keyframe2 = new KeyFrame()
            {
                Setters =
                {
                    new Setter(Border.WidthProperty, 0d),
                },
                KeyTime = TimeSpan.FromSeconds(0)
            };

            var animation = new Animation()
            {
                Duration = TimeSpan.FromSeconds(1),
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
            Assert.Equal(border.Width, 0d);

            clock.Step(TimeSpan.FromSeconds(1)); 
            Assert.Equal(border.Width, 100d);
 
        }


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

        [Fact]
        public void Check_FillModes_Start_and_End_Values_if_Retained()
        {
            var keyframe1 = new KeyFrame()
            {
                Setters =
                {
                    new Setter(Border.WidthProperty, 0d),
                },
                Cue = new Cue(0.0d)
            };

            var keyframe2 = new KeyFrame()
            {
                Setters =
                {
                    new Setter(Border.WidthProperty, 300d),
                },
                Cue = new Cue(1.0d)
            };

            var animation = new Animation()
            {
                Duration = TimeSpan.FromSeconds(0.05d),
                Delay = TimeSpan.FromSeconds(0.05d),
                Easing = new SineEaseInOut(),
                FillMode = FillMode.Both,
                Children =
                {
                    keyframe1,
                    keyframe2
                }
            };

            var border = new Border()
            {
                Height = 100d,
                Width = 100d,
            };

            var clock = new TestClock();
            var animationRun = animation.RunAsync(border, clock);

            clock.Step(TimeSpan.FromSeconds(0d));
            Assert.Equal(border.Width, 0d);

            clock.Step(TimeSpan.FromSeconds(0.050d));
            Assert.Equal(border.Width, 0d);

            clock.Step(TimeSpan.FromSeconds(0.100d));
            Assert.Equal(border.Width, 300d);
        }

        [Fact(Skip = "See #6111")]
        public void Dispose_Subscription_Should_Stop_Animation()
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
                Duration = TimeSpan.FromSeconds(10),
                Delay = TimeSpan.FromSeconds(0),
                DelayBetweenIterations = TimeSpan.FromSeconds(0),
                IterationCount = new IterationCount(1),
                Children =
                {
                    keyframe2,
                    keyframe1
                }
            };

            var border = new Border()
            {
                Height = 100d,
                Width = 50d
            };
            var propertyChangedCount = 0;
            var animationCompletedCount = 0;
            border.PropertyChanged += (sender, e) =>
            {
                if (e.Property == Control.WidthProperty)
                {
                    propertyChangedCount++;
                }
            };

            var clock = new TestClock();
            var disposable = animation.Apply(border, clock, Observable.Return(true), () => animationCompletedCount++);

            Assert.Equal(0, propertyChangedCount);

            clock.Step(TimeSpan.FromSeconds(0));
            Assert.Equal(0, animationCompletedCount);
            Assert.Equal(1, propertyChangedCount);

            disposable.Dispose();

            // Clock ticks should be ignored after Dispose
            clock.Step(TimeSpan.FromSeconds(5));
            clock.Step(TimeSpan.FromSeconds(6));
            clock.Step(TimeSpan.FromSeconds(7));

            // On animation disposing (cancellation) on completed is not invoked (is it expected?)
            Assert.Equal(0, animationCompletedCount);
            // Initial property changed before cancellation + animation value removal.
            Assert.Equal(2, propertyChangedCount);
        }

        [Fact]
        public void Do_Not_Run_Cancelled_Animation()
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
                Duration = TimeSpan.FromSeconds(10),
                Delay = TimeSpan.FromSeconds(0),
                DelayBetweenIterations = TimeSpan.FromSeconds(0),
                IterationCount = new IterationCount(1),
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
            var propertyChangedCount = 0;
            border.PropertyChanged += (sender, e) =>
            {
                if (e.Property == Control.WidthProperty)
                {
                    propertyChangedCount++;
                }
            };

            var clock = new TestClock();
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            var animationRun = animation.RunAsync(border, clock, cancellationTokenSource.Token);

            clock.Step(TimeSpan.FromSeconds(10));
            Assert.Equal(0, propertyChangedCount);
            Assert.True(animationRun.IsCompleted);
        }

        [Fact(Skip = "See #6111")]
        public void Cancellation_Should_Stop_Animation()
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
                Duration = TimeSpan.FromSeconds(10),
                Delay = TimeSpan.FromSeconds(0),
                DelayBetweenIterations = TimeSpan.FromSeconds(0),
                IterationCount = new IterationCount(1),
                Children =
                {
                    keyframe2,
                    keyframe1
                }
            };

            var border = new Border()
            {
                Height = 100d,
                Width = 50d
            };
            var propertyChangedCount = 0;
            border.PropertyChanged += (sender, e) =>
            {
                if (e.Property == Control.WidthProperty)
                {
                    propertyChangedCount++;
                }
            };

            var clock = new TestClock();
            var cancellationTokenSource = new CancellationTokenSource();
            var animationRun = animation.RunAsync(border, clock, cancellationTokenSource.Token);

            Assert.Equal(0, propertyChangedCount);

            clock.Step(TimeSpan.FromSeconds(0));
            Assert.False(animationRun.IsCompleted);
            Assert.Equal(1, propertyChangedCount);

            cancellationTokenSource.Cancel();
            clock.Step(TimeSpan.FromSeconds(1));
            clock.Step(TimeSpan.FromSeconds(2));
            clock.Step(TimeSpan.FromSeconds(3));
            //Assert.Equal(2, propertyChangedCount);

            animationRun.Wait();

            clock.Step(TimeSpan.FromSeconds(6));
            Assert.True(animationRun.IsCompleted);
            Assert.Equal(2, propertyChangedCount);
        }

        [Fact]
        public void Cancellation_Of_Completed_Animation_Does_Not_Fail()
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
                Duration = TimeSpan.FromSeconds(10),
                Delay = TimeSpan.FromSeconds(0),
                DelayBetweenIterations = TimeSpan.FromSeconds(0),
                IterationCount = new IterationCount(1),
                Children =
                {
                    keyframe2,
                    keyframe1
                }
            };

            var border = new Border()
            {
                Height = 100d,
                Width = 50d
            };
            var propertyChangedCount = 0;
            border.PropertyChanged += (sender, e) =>
            {
                if (e.Property == Control.WidthProperty)
                {
                    propertyChangedCount++;
                }
            };

            var clock = new TestClock();
            var cancellationTokenSource = new CancellationTokenSource();
            var animationRun = animation.RunAsync(border, clock, cancellationTokenSource.Token);

            Assert.Equal(0, propertyChangedCount);

            clock.Step(TimeSpan.FromSeconds(0));
            Assert.False(animationRun.IsCompleted);
            Assert.Equal(1, propertyChangedCount);

            clock.Step(TimeSpan.FromSeconds(10));
            Assert.True(animationRun.IsCompleted);
            Assert.Equal(2, propertyChangedCount);

            cancellationTokenSource.Cancel();
            animationRun.Wait();
        }
    }
}
