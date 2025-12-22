using System;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.LeakTests
{
    public class TransitionTests : ScopedTestBase
    {
        [Fact]
        public void Transition_On_StyledProperty_Is_Freed()
        {
            var clock = new MockGlobalClock();

            using (UnitTestApplication.Start(TestServices.StyledWindow.With(globalClock: clock)))
            {
                WeakReference Run()
                {
                    var opacityTransition = new DoubleTransition { Duration = TimeSpan.FromSeconds(1), Property = Border.OpacityProperty, };

                    var border = new Border { Transitions = new Transitions() { opacityTransition } };
                    var window = new Window();
                    window.Content = border;
                    window.Show();

                    border.Opacity = 0;

                    clock.Pulse(TimeSpan.FromSeconds(0));
                    clock.Pulse(TimeSpan.FromSeconds(0.5));

                    Assert.Equal(0.5, border.Opacity);

                    var transitionInstance = border.TryGetTransitionInstance(opacityTransition);
                    Assert.NotNull(transitionInstance);

                    clock.Pulse(TimeSpan.FromSeconds(1));

                    Assert.Equal(0, border.Opacity);

                    window.Close();

                    return new WeakReference(transitionInstance);
                }

                var weakTransitionInstance = Run();
                Assert.True(weakTransitionInstance.IsAlive);

                CollectGarbage();

                Assert.False(weakTransitionInstance.IsAlive);
            }
        }

        [Fact]
        public void Shared_Transition_Collection_Is_Not_Leaking()
        {
            var clock = new MockGlobalClock();
            using (UnitTestApplication.Start(TestServices.StyledWindow.With(globalClock: clock)))
            {
                // Our themes do share transition collections, so we need to test this scenario well.
                var sharedTransitions = new Transitions
                {
                    new TransformOperationsTransition
                    {
                        Property = Visual.RenderTransformProperty, Duration = TimeSpan.FromSeconds(0.750)
                    }
                };
                var controlTheme = new ControlTheme(typeof(Button))
                {
                    BasedOn = Application.Current?.Resources[typeof(Button)] as ControlTheme,
                    Setters = { new Setter(Animatable.TransitionsProperty, sharedTransitions) }
                };

                WeakReference Run()
                {
                    var button = new Button() { Theme = controlTheme };
                    var window = new Window();
                    window.Content = button;
                    window.Show();
                    window.Content = null;
                    window.Close();

                    return new WeakReference(button);
                }

                var weakButton = Run();
                Assert.True(weakButton.IsAlive);

                CollectGarbage();

                Assert.False(weakButton.IsAlive);
            }
        }

        [Fact]
        public void Lazily_Created_Control_Should_Not_Leak_Transitions()
        {
            var clock = new MockGlobalClock();
            using (UnitTestApplication.Start(TestServices.StyledWindow.With(globalClock: clock)))
            {
                var sharedTransitions = new Transitions
                {
                    new TransformOperationsTransition
                    {
                        Property = Visual.RenderTransformProperty, Duration = TimeSpan.FromSeconds(0.750)
                    }
                };
                var controlTheme = new ControlTheme(typeof(Button))
                {
                    BasedOn = Application.Current?.Resources[typeof(Button)] as ControlTheme,
                    Setters = { new Setter(Animatable.TransitionsProperty, sharedTransitions) }
                };

                WeakReference Run()
                {
                    var window = new Window();
                    window.Show();
                    var button = new Button() { Theme = controlTheme };
                    window.Content = new UserControl
                    {
                        Content = button,
                        // When invisible, Button won't be attached to the visual tree
                        IsVisible = false
                    };
                    window.Content = null;
                    window.Close();

                    return new WeakReference(button);
                }

                var weakButton = Run();
                Assert.True(weakButton.IsAlive);

                CollectGarbage();

                Assert.False(weakButton.IsAlive);
            }
        }

        private static void CollectGarbage()
        {
            // Process all Loaded events to free control reference(s)
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
            GC.Collect();
        }
    }
}
