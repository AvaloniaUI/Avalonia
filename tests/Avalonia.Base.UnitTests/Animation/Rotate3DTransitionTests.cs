using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Animation
{
    public class Rotate3DTransitionTests
    {
        [Fact]
        public async Task Horizontal_Rotate3DTransition_Slides_Forward_From_One_Control_To_The_Other()
        {
            using var app = Start();
            var clock = GetMockGlobalClock();
            var target = new Rotate3DTransition { Duration = TimeSpan.FromSeconds(1) };
            var (from, to) = CreateControls();
            var cancel = new CancellationTokenSource();
            var task = target.Start(from, to, true, cancel.Token);
            var time = 0;
            var fromAngleY = 0.0;
            var toAngleY = 90.0;

            while (time < 1000)
            {
                clock.Pulse(TimeSpan.FromMilliseconds(time));

                Assert.Equal(fromAngleY, from.GetValue(Rotate3DTransform.AngleYProperty), 0.001);
                Assert.Equal(toAngleY, to.GetValue(Rotate3DTransform.AngleYProperty), 0.001);

                time += 100;
                fromAngleY -= 9;
                toAngleY -= 9;
            }

            clock.Pulse(TimeSpan.FromMilliseconds(1000));

            // This transition requires an async await here whereas CrossFade and PageSlide have a
            // completed task as this point. Not sure why.
            await task;

            Assert.False(from.IsVisible);
            Assert.True(to.IsVisible);
            Assert.False(from.IsSet(Rotate3DTransform.AngleYProperty));
            Assert.False(to.IsSet(Rotate3DTransform.AngleYProperty));
        }

        [Fact]
        public void From_Control_Should_Not_Flicker()
        {
            // Issue #11167
            using var app = Start();
            var clock = GetMockGlobalClock();
            var target = new Rotate3DTransition { Duration = TimeSpan.FromSeconds(1) };
            var (from, to) = CreateControls();
            var cancel = new CancellationTokenSource();
            var task = target.Start(from, to, true, cancel.Token);
            var time = 0;
            var fromState = new List<(double x, bool isVisible)>();

            from.PropertyChanged += (s, e) =>
            {
                if (e.Property == Rotate3DTransform.AngleYProperty)
                    fromState.Add((e.GetNewValue<double>(), from.IsVisible));
            };

            // Run the animation until the penultimate frame.
            while (time < 1000)
            {
                clock.Pulse(TimeSpan.FromMilliseconds(time));
                time += 100;
            }

            Assert.Equal(9, fromState.Count);

            // Check that from Y angle is decreasing and control is visible.
            for (var i = 0; i < fromState.Count - 1; ++i)
            {
                Assert.True(fromState[i].x > fromState[i + 1].x);
                Assert.True(fromState[i].isVisible);
                Assert.True(fromState[i + 1].isVisible);
            }

            // Run the last frame.
            clock.Pulse(TimeSpan.FromMilliseconds(time));

            // Check that Y angle is reset to default value (0.0) but control is not visible.
            Assert.Equal(10, fromState.Count);
            Assert.Equal((0.0, false), fromState[9]);
        }

        [Fact]
        public void Rotate3DTransition_Can_Be_Canceled()
        {
            using var app = Start();
            var clock = GetMockGlobalClock();
            var target = new Rotate3DTransition { Duration = TimeSpan.FromSeconds(1) };
            var (from, to) = CreateControls();
            var cancel = new CancellationTokenSource();
            var task = target.Start(from, to, true, cancel.Token);
            var time = 0;
            var fromAngleY = 0.0;
            var toAngleY = 90.0;

            while (time < 500)
            {
                clock.Pulse(TimeSpan.FromMilliseconds(time));

                Assert.Equal(fromAngleY, from.GetValue(Rotate3DTransform.AngleYProperty), 0.001);
                Assert.Equal(toAngleY, to.GetValue(Rotate3DTransform.AngleYProperty), 0.001);

                time += 100;
                fromAngleY -= 9;
                toAngleY -= 9;
            }

            cancel.Cancel();

            Assert.True(task.IsCompleted);
            Assert.True(from.IsVisible);
            Assert.True(to.IsVisible);
            Assert.False(from.IsSet(Rotate3DTransform.AngleYProperty));
            Assert.False(to.IsSet(Rotate3DTransform.AngleYProperty));
        }

        private static IDisposable Start()
        {
            var clock = new MockGlobalClock();
            var services = new TestServices(globalClock: clock);
            return UnitTestApplication.Start(services);
        }

        private (Border from, Canvas to) CreateControls()
        {
            var from = new Border();
            var to = new Canvas();
            var panel = new Panel { Children = { from, to } };
            var root = new TestRoot(panel)
            {
                Width = 1000,
                Height = 1000,
            };
            root.LayoutManager.ExecuteInitialLayoutPass();
            return (from, to);
        }

        private static MockGlobalClock GetMockGlobalClock()
        {
            return Assert.IsType<MockGlobalClock>(AvaloniaLocator.Current.GetService<IGlobalClock>());
        }
    }
}
