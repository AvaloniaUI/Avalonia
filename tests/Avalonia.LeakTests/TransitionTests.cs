using System;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.UnitTests;
using JetBrains.dotMemoryUnit;
using Xunit;
using Xunit.Abstractions;

namespace Avalonia.LeakTests
{
    [DotMemoryUnit(FailIfRunWithoutSupport = false)]
    public class TransitionTests
    {
        public TransitionTests(ITestOutputHelper atr)
        {
            DotMemoryUnitTestOutput.SetOutputMethod(atr.WriteLine);
        }

        [Fact(Skip = "TODO: Fix this leak")]
        public void Transition_On_StyledProperty_Is_Freed()
        {
            var clock = new MockGlobalClock();

            using (UnitTestApplication.Start(new TestServices(globalClock: clock)))
            {
                Func<Border> run = () =>
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
                    clock.Pulse(TimeSpan.FromSeconds(0.5));

                    Assert.Equal(0.5, border.Opacity);

                    clock.Pulse(TimeSpan.FromSeconds(1));

                    Assert.Equal(0, border.Opacity);
                    return border;
                };

                var result = run();

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<TransitionInstance>()).ObjectsCount));
            }
        }
    }
}
