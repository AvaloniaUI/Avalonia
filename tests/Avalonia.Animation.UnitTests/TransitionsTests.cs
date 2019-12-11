using System;
using Avalonia.Controls;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Animation.UnitTests
{
    public class TransitionsTests
    {
        [Fact]
        public void Check_Transitions_Interpolation_Negative_Bounds_Clamp()
        {
            var clock = new MockGlobalClock();

            using (UnitTestApplication.Start(new TestServices(globalClock: clock)))
            {
                var border = new Border
                {
                    Transitions =
                    {
                        new DoubleTransition
                        {
                            Duration = TimeSpan.FromSeconds(1),
                            Property = Border.OpacityProperty,
                        }
                    }
                };

                border.Opacity = 0;

                clock.Pulse(TimeSpan.FromSeconds(0));
                clock.Pulse(TimeSpan.FromSeconds(-0.5));

                Assert.Equal(0, border.Opacity);
            }
        }

        [Fact]
        public void Check_Transitions_Interpolation_Positive_Bounds_Clamp()
        {
            var clock = new MockGlobalClock();

            using (UnitTestApplication.Start(new TestServices(globalClock: clock)))
            {
                var border = new Border
                {
                    Transitions =
                    {
                        new DoubleTransition
                        {
                            Duration = TimeSpan.FromSeconds(1),
                            Property = Border.OpacityProperty,
                        }
                    }
                };

                border.Opacity = 0;

                clock.Pulse(TimeSpan.FromSeconds(0));
                clock.Pulse(TimeSpan.FromMilliseconds(1001));

                Assert.Equal(0, border.Opacity);
            }
        }

        [Fact]
        public void TransitionInstance_With_Zero_Duration_Is_Completed_On_First_Tick()
        {
            var clock = new MockGlobalClock();

            using (UnitTestApplication.Start(new TestServices(globalClock: clock)))
            {
                int i = 0;
                var inst = new TransitionInstance(clock, TimeSpan.Zero).Subscribe(nextValue =>
                {
                    switch (i++)
                    {
                        case 0: Assert.Equal(0, nextValue); break;
                        case 1: Assert.Equal(1d, nextValue); break;
                    }
                });

                clock.Pulse(TimeSpan.FromMilliseconds(10));
            }
        }
    }
}
