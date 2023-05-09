using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Animation
{
    public class PageSlideTests
    {
        [Fact]
        public void Horizontal_PageSlide_Slides_Forward_From_One_Control_To_The_Other()
        {
            using var app = Start();
            var clock = GetMockGlobalClock();
            var target = new PageSlide { Duration = TimeSpan.FromSeconds(1) };
            var (from, to) = CreateControls();
            var cancel = new CancellationTokenSource();
            var task = target.Start(from, to, true, cancel.Token);
            var time = 0;
            var fromTranslate = 0.0;
            var toTranslate = 1000.0;

            while (time < 1000)
            {
                clock.Pulse(TimeSpan.FromMilliseconds(time));

                Assert.Equal(fromTranslate, from.GetValue(TranslateTransform.XProperty), 0.001);
                Assert.Equal(toTranslate, to.GetValue(TranslateTransform.XProperty), 0.001);

                time += 100;
                fromTranslate -= 100;
                toTranslate -= 100;
            }

            clock.Pulse(TimeSpan.FromMilliseconds(1000));

            Assert.True(task.IsCompleted);
            Assert.False(from.IsVisible);
            Assert.True(to.IsVisible);
            Assert.False(from.IsSet(TranslateTransform.XProperty));
            Assert.False(to.IsSet(TranslateTransform.XProperty));
        }

        [Fact]
        public void From_Control_Should_Not_Flicker()
        {
            // Issue #11167
            using var app = Start();
            var clock = GetMockGlobalClock();
            var target = new PageSlide { Duration = TimeSpan.FromSeconds(1) };
            var (from, to) = CreateControls();
            var cancel = new CancellationTokenSource();
            var task = target.Start(from, to, true, cancel.Token);
            var time = 0;
            var fromState = new List<(double x, bool isVisible)>();

            from.PropertyChanged += (s, e) =>
            {
                if (e.Property == TranslateTransform.XProperty)
                    fromState.Add((e.GetNewValue<double>(), from.IsVisible));
            };

            // Run the animation until the penultimate frame.
            while (time < 1000)
            {
                clock.Pulse(TimeSpan.FromMilliseconds(time));
                time += 100;
            }

            Assert.Equal(9, fromState.Count);

            // Check that from X translate is decreasing and control is visible.
            for (var i = 0; i < fromState.Count - 1; ++i)
            {
                Assert.True(fromState[i].x > fromState[i + 1].x);
                Assert.True(fromState[i].isVisible);
                Assert.True(fromState[i + 1].isVisible);
            }

            // Run the last frame.
            clock.Pulse(TimeSpan.FromMilliseconds(time));

            // Check that X translate is reset to default value (0.0) but control is not visible.
            Assert.Equal(10, fromState.Count);
            Assert.Equal((0.0, false), fromState[9]);
        }

        [Fact]
        public void PageSlide_Can_Be_Canceled()
        {
            using var app = Start();
            var clock = GetMockGlobalClock();
            var target = new PageSlide { Duration = TimeSpan.FromSeconds(1) };
            var (from, to) = CreateControls();
            var cancel = new CancellationTokenSource();
            var task = target.Start(from, to, true, cancel.Token);
            var time = 0;
            var fromTranslate = 0.0;
            var toTranslate = 1000.0;

            while (time < 500)
            {
                clock.Pulse(TimeSpan.FromMilliseconds(time));

                Assert.Equal(fromTranslate, from.GetValue(TranslateTransform.XProperty), 0.001);
                Assert.Equal(toTranslate, to.GetValue(TranslateTransform.XProperty), 0.001);

                time += 100;
                fromTranslate -= 100;
                toTranslate -= 100;
            }

            cancel.Cancel();

            Assert.True(task.IsCompleted);
            Assert.True(from.IsVisible);
            Assert.True(to.IsVisible);
            Assert.False(from.IsSet(TranslateTransform.XProperty));
            Assert.False(to.IsSet(TranslateTransform.XProperty));
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
