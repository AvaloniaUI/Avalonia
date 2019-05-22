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
    }
}
