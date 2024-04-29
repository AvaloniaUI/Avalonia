using System;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Styling;
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

        [Fact]
        public void Transition_On_StyledProperty_Is_Freed()
        {
            var clock = new MockGlobalClock();

            using (UnitTestApplication.Start(TestServices.StyledWindow.With(globalClock: clock)))
            {
                Func<Border> run = () =>
                {
                    var border = new Border
                    {
                        Transitions = new Transitions()
                        {
                            new DoubleTransition
                            {
                                Duration = TimeSpan.FromSeconds(1),
                                Property = Border.OpacityProperty,
                            }
                        }
                    };
                    var window = new Window();
                    window.Content = border;
                    window.Show();

                    border.Opacity = 0;

                    clock.Pulse(TimeSpan.FromSeconds(0));
                    clock.Pulse(TimeSpan.FromSeconds(0.5));

                    Assert.Equal(0.5, border.Opacity);

                    clock.Pulse(TimeSpan.FromSeconds(1));

                    Assert.Equal(0, border.Opacity);

                    window.Close();

                    return border;
                };

                var result = run();

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<TransitionInstance>()).ObjectsCount));
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
                
                Func<Window> run = () =>
                {
                    var button = new Button() { Theme = controlTheme };
                    var window = new Window();
                    window.Content = button;
                    window.Show();
                    window.Content = null;
                    window.Close();

                    return window;
                };

                var result = run();

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<Button>()).ObjectsCount));
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
                
                Func<Window> run = () =>
                {
                    var window = new Window();
                    window.Show();
                    window.Content = new UserControl
                    {
                        Content = new Button() { Theme = controlTheme },
                        // When invisible, Button won't be attached to the visual tree
                        IsVisible = false
                    };
                    window.Content = null;
                    window.Close();

                    return window;
                };

                var result = run();

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<Button>()).ObjectsCount));
            }
        }
    }
}
