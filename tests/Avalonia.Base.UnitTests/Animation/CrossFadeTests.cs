using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Animation
{
    public class CrossFadeTests
    {
        [Fact]
        public void CrossFade_Fades_From_One_Control_To_The_Other()
        {
            using var app = Start();
            var clock = GetMockGlobalClock();
            var target = new CrossFade { Duration = TimeSpan.FromSeconds(1) };
            var from = new Border();
            var to = new Canvas();
            var cancel = new CancellationTokenSource();

            Assert.Equal(1, from.Opacity);
            Assert.Equal(1, to.Opacity);

            var task = target.Start(from, to, cancel.Token);
            var time = 0;
            var fromOpacity = 1.0;
            var toOpacity = 0.0;

            while (time < 1000)
            {
                clock.Pulse(TimeSpan.FromMilliseconds(time));

                Assert.Equal(fromOpacity, from.Opacity, 0.001);
                Assert.Equal(toOpacity, to.Opacity, 0.001);

                time += 100;
                fromOpacity -= 0.1;
                toOpacity += 0.1;
            }

            clock.Pulse(TimeSpan.FromMilliseconds(1000));

            Assert.True(task.IsCompleted);
            Assert.False(from.IsVisible);
            Assert.True(to.IsVisible);
            Assert.Equal(from.Opacity, 1);
            Assert.Equal(to.Opacity, 1);
        }

        [Fact]
        public void From_Control_Should_Not_Flicker()
        {
            // Issue #11167
            using var app = Start();
            var clock = GetMockGlobalClock();
            var target = new CrossFade { Duration = TimeSpan.FromSeconds(1) };
            var from = new Border();
            var to = new Canvas();
            var cancel = new CancellationTokenSource();

            Assert.Equal(1, from.Opacity);
            Assert.Equal(1, to.Opacity);

            var task = target.Start(from, to, cancel.Token);
            var time = 0;
            var fromState = new List<(double opacity, bool isVisible)>();

            from.PropertyChanged += (s, e) =>
            {
                if (e.Property == Visual.IsVisibleProperty)
                    fromState.Add((to.Opacity, e.GetNewValue<bool>()));
                if (e.Property == Visual.OpacityProperty)
                    fromState.Add((e.GetNewValue<double>(), from.IsVisible));
            };

            // Run the animation until the penultimate frame.
            while (time < 1000)
            {
                clock.Pulse(TimeSpan.FromMilliseconds(time));
                time += 100;
            }

            Assert.Equal(9, fromState.Count);

            // Check that fromOpacity is decreasing and control is visible.
            for (var i = 0; i < fromState.Count - 1; ++i)
            {
                Assert.True(fromState[i].opacity > fromState[i + 1].opacity);
                Assert.True(fromState[i].isVisible);
                Assert.True(fromState[i + 1].isVisible);
            }

            // Run the last frame.
            clock.Pulse(TimeSpan.FromMilliseconds(time));

            // Control should be hidden before transparency being reset.
            Assert.Equal(11, fromState.Count);
            Assert.Equal((0.9, false), fromState[9]);
            Assert.Equal((1.0, false), fromState[10]);
        }

        [Fact]
        public void CrossFade_Can_Be_Canceled()
        {
            using var app = Start();
            var clock = GetMockGlobalClock();
            var target = new CrossFade { Duration = TimeSpan.FromSeconds(1) };
            var from = new Border();
            var to = new Canvas();
            var cancel = new CancellationTokenSource();

            Assert.Equal(1, from.Opacity);
            Assert.Equal(1, to.Opacity);

            var task = target.Start(from, to, cancel.Token);
            var time = 0;
            var fromOpacity = 1.0;
            var toOpacity = 0.0;

            while (time < 500)
            {
                clock.Pulse(TimeSpan.FromMilliseconds(time));

                Assert.Equal(fromOpacity, from.Opacity, 0.001);
                Assert.Equal(toOpacity, to.Opacity, 0.001);

                time += 100;
                fromOpacity -= 0.1;
                toOpacity += 0.1;
            }

            cancel.Cancel();

            Assert.True(task.IsCompleted);
            Assert.True(from.IsVisible);
            Assert.True(to.IsVisible);
            Assert.Equal(from.Opacity, 1);
            Assert.Equal(to.Opacity, 1);
        }

        [Fact]
        public void CrossFade_To_Invisible_Control_Should_Make_Control_Transparent_Before_Visible()
        {
            // Issue #11167
            using var app = Start();
            var clock = GetMockGlobalClock();
            var target = new CrossFade { Duration = TimeSpan.FromSeconds(1) };
            var to = new Canvas { IsVisible = false };
            var cancel = new CancellationTokenSource();

            Assert.Equal(1, to.Opacity);

            var toState = new List<(double opacity, bool isVisible)>();

            to.PropertyChanged += (s, e) =>
            {
                if (e.Property == Visual.IsVisibleProperty)
                    toState.Add((to.Opacity, e.GetNewValue<bool>()));
                if (e.Property == Visual.OpacityProperty)
                    toState.Add((e.GetNewValue<double>(), to.IsVisible));
            };

            var task = target.Start(null, to, cancel.Token);
            var time = 0;

            // Run the animation finish.
            while (time <= 1000)
            {
                clock.Pulse(TimeSpan.FromMilliseconds(time));
                time += 100;
            }

            // Control should be made transparent before shown.
            Assert.Equal((0.0, false), toState[0]);
            Assert.Equal((0.0, true), toState[1]);

            // Control should remain visible.
            Assert.True(task.IsCompleted);
            Assert.True(to.IsVisible);
        }

        private static IDisposable Start()
        {
            var clock = new MockGlobalClock();
            var services = new TestServices(globalClock: clock);
            return UnitTestApplication.Start(services);
        }

        private static MockGlobalClock GetMockGlobalClock()
        {
            return Assert.IsType<MockGlobalClock>(AvaloniaLocator.Current.GetService<IGlobalClock>());
        }
    }
}
