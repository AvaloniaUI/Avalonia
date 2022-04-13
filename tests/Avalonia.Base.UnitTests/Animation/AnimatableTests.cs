﻿using System;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Animation
{
    public class AnimatableTests
    {
        [Fact]
        public void Transition_Is_Not_Applied_When_Not_Attached_To_Visual_Tree()
        {
            var target = CreateTarget();
            var control = new Control
            {
                Transitions = new Transitions { target.Object },
            };

            control.Opacity = 0.5;

            target.Verify(x => x.Apply(
                control,
                It.IsAny<IClock>(),
                1.0,
                0.5),
                Times.Never);
        }

        [Fact]
        public void Transition_Is_Not_Applied_To_Initial_Style()
        {
            using (Start())
            {
                var target = CreateTarget();
                var control = new Control
                {
                    Transitions = new Transitions { target.Object },
                };

                var root = new TestRoot
                {
                    Styles =
                    {
                        new Style(x => x.OfType<Control>())
                        {
                            Setters =
                            {
                                new Setter(Visual.OpacityProperty, 0.8),
                            }
                        }
                    }
                };

                root.Child = control;

                Assert.Equal(0.8, control.Opacity);

                target.Verify(x => x.Apply(
                    It.IsAny<Control>(),
                    It.IsAny<IClock>(),
                    It.IsAny<object>(),
                    It.IsAny<object>()),
                    Times.Never);
            }
        }

        [Fact]
        public void Transition_Is_Applied_When_Local_Value_Changes()
        {
            using var app = Start();
            var target = CreateTarget();
            var control = CreateControl(target.Object);

            control.Opacity = 0.5;

            target.Verify(x => x.Apply(
                control,
                It.IsAny<IClock>(),
                1.0,
                0.5));
        }

        [Fact]
        public void Transition_Is_Not_Applied_When_Animated_Value_Changes()
        {
            var target = CreateTarget();
            var control = CreateControl(target.Object);

            control.SetValue(Visual.OpacityProperty, 0.5, BindingPriority.Animation);

            target.Verify(x => x.Apply(
                control,
                It.IsAny<IClock>(),
                1.0,
                0.5),
                Times.Never);
        }


        [Theory]
        [InlineData(null)] //null value
        [InlineData("stringValue")] //string value
        public void Invalid_Values_In_Animation_Should_Not_Crash_Animations(object invalidValue)
        {
            var keyframe1 = new KeyFrame()
            {
                Setters =
                {
                    new Setter(Layoutable.WidthProperty, 1d),
                },
                KeyTime = TimeSpan.FromSeconds(0)
            };

            var keyframe2 = new KeyFrame()
            {
                Setters =
                {
                    new Setter(Layoutable.WidthProperty, 2d),
                },
                KeyTime = TimeSpan.FromSeconds(2),
            };

            var keyframe3 = new KeyFrame()
            {
                Setters =
                {
                    new Setter(Layoutable.WidthProperty, invalidValue),
                },
                KeyTime = TimeSpan.FromSeconds(3),
            };

            var animation = new Avalonia.Animation.Animation()
            {
                Duration = TimeSpan.FromSeconds(3),
                Children =
                {
                    keyframe1,
                    keyframe2,
                    keyframe3
                },
                IterationCount = new IterationCount(5),
                PlaybackDirection = PlaybackDirection.Alternate,
            };

            var rect = new Rectangle()
            {
                Width = 11,
            };

            var originalValue = rect.Width;

            var clock = new TestClock();
            var animationRun = animation.RunAsync(rect, clock);

            clock.Step(TimeSpan.Zero);
            Assert.Equal(rect.Width, 1);
            clock.Step(TimeSpan.FromSeconds(2));
            Assert.Equal(rect.Width, 2);
            clock.Step(TimeSpan.FromSeconds(3));
            //here we have invalid value so value should be expected and set to initial original value
            Assert.Equal(rect.Width, originalValue);
        }

        [Fact]
        public void Transition_Is_Not_Applied_When_StyleTrigger_Changes_With_LocalValue_Present()
        {
            using var app = Start();
            var target = CreateTarget();
            var control = CreateControl(target.Object);

            control.SetValue(Visual.OpacityProperty, 0.5);

            target.Verify(x => x.Apply(
                control,
                It.IsAny<IClock>(),
                1.0,
                0.5));
            target.Invocations.Clear();

            control.SetValue(Visual.OpacityProperty, 0.8, BindingPriority.StyleTrigger);

            target.Verify(x => x.Apply(
                It.IsAny<Control>(),
                It.IsAny<IClock>(),
                It.IsAny<object>(),
                It.IsAny<object>()),
                Times.Never);
        }

        [Fact]
        public void Transition_Is_Disposed_When_Local_Value_Changes()
        {
            using var app = Start();
            var target = CreateTarget();
            var control = CreateControl(target.Object);
            var sub = new Mock<IDisposable>();

            target.Setup(x => x.Apply(control, It.IsAny<IClock>(), 1.0, 0.5)).Returns(sub.Object);

            control.Opacity = 0.5;
            sub.Invocations.Clear();
            control.Opacity = 0.4;

            sub.Verify(x => x.Dispose());
        }

        [Fact]
        public void New_Transition_Is_Applied_When_Local_Value_Changes()
        {
            using var app = Start();
            var target = CreateTarget();
            var control = CreateControl(target.Object);

            target.Setup(x => x.Property).Returns(Visual.OpacityProperty);
            target.Setup(x => x.Apply(control, It.IsAny<IClock>(), 1.0, 0.5))
                .Callback(() =>
                {
                    control.SetValue(Visual.OpacityProperty, 0.9, BindingPriority.Animation);
                })
                .Returns(Mock.Of<IDisposable>());

            control.Opacity = 0.5;

            Assert.Equal(0.9, control.Opacity);
            target.Invocations.Clear();

            control.Opacity = 0.4;

            target.Verify(x => x.Apply(
                control,
                It.IsAny<IClock>(),
                0.9,
                0.4));
        }

        [Fact]
        public void Transition_Is_Not_Applied_When_Removed_From_Visual_Tree()
        {
            using var app = Start();
            var target = CreateTarget();
            var control = CreateControl(target.Object);

            control.Opacity = 0.5;

            target.Verify(x => x.Apply(
                control,
                It.IsAny<IClock>(),
                1.0,
                0.5));
            target.Invocations.Clear();

            var root = (TestRoot)control.Parent;
            root.Child = null;
            control.Opacity = 0.8;

            target.Verify(x => x.Apply(
                It.IsAny<Control>(),
                It.IsAny<IClock>(),
                It.IsAny<object>(),
                It.IsAny<object>()),
                Times.Never);
        }

        [Fact]
        public void Animation_Is_Cancelled_When_Transition_Removed()
        {
            using var app = Start();
            var target = CreateTarget();
            var control = CreateControl(target.Object);
            var sub = new Mock<IDisposable>();

            target.Setup(x => x.Apply(
                It.IsAny<Animatable>(),
                It.IsAny<IClock>(),
                It.IsAny<object>(),
                It.IsAny<object>())).Returns(sub.Object);

            control.Opacity = 0.5;
            control.Transitions.RemoveAt(0);

            sub.Verify(x => x.Dispose());
        }

        [Fact]
        public void Animation_Is_Cancelled_When_New_Style_Activates()
        {
            using (Start())
            {
                var target = CreateTarget();
                var control = CreateStyledControl(target.Object);
                var sub = new Mock<IDisposable>();

                target.Setup(x => x.Apply(
                    control,
                    It.IsAny<IClock>(),
                    1.0,
                    0.5)).Returns(sub.Object);

                control.Opacity = 0.5;

                target.Verify(x => x.Apply(
                    control,
                    It.IsAny<IClock>(),
                    1.0,
                    0.5),
                    Times.Once);

                control.Classes.Add("foo");

                sub.Verify(x => x.Dispose());
            }
        }

        [Fact]
        public void Transition_From_Style_Trigger_Is_Applied()
        {
            using (Start())
            {
                var target = CreateTransition(Control.WidthProperty);
                var control = CreateStyledControl(transition2: target.Object);
                var sub = new Mock<IDisposable>();

                control.Classes.Add("foo");
                control.Width = 100;

                target.Verify(x => x.Apply(
                    control,
                    It.IsAny<IClock>(),
                    double.NaN,
                    100.0),
                    Times.Once);
            }
        }

        [Fact]
        public void Replacing_Transitions_During_Animation_Does_Not_Throw_KeyNotFound()
        {
            // Issue #4059
            using (Start())
            {
                Border target;
                var clock = new TestClock();
                var root = new TestRoot
                {
                    Clock = clock,
                    Styles =
                    {
                        new Style(x => x.OfType<Border>())
                        {
                            Setters =
                            {
                                new Setter(Border.TransitionsProperty,
                                    new Transitions
                                    {
                                        new DoubleTransition
                                        {
                                            Property = Border.OpacityProperty,
                                            Duration = TimeSpan.FromSeconds(1),
                                        },
                                    }),
                            },
                        },
                        new Style(x => x.OfType<Border>().Class("foo"))
                        {
                            Setters =
                            {
                                new Setter(Border.TransitionsProperty,
                                    new Transitions
                                    {
                                        new DoubleTransition
                                        {
                                            Property = Border.OpacityProperty,
                                            Duration = TimeSpan.FromSeconds(1),
                                        },
                                    }),
                                new Setter(Border.OpacityProperty, 0.0),
                            },
                        },
                    },
                    Child = target = new Border
                    {
                        Background = Brushes.Red,
                    }
                };

                root.Measure(Size.Infinity);
                root.Arrange(new Rect(root.DesiredSize));

                target.Classes.Add("foo");
                clock.Step(TimeSpan.FromSeconds(0));
                clock.Step(TimeSpan.FromSeconds(0.5));

                Assert.Equal(0.5, target.Opacity);

                target.Classes.Remove("foo");
            }
        }

        [Fact]
        public void Transitions_Can_Be_Changed_To_Collection_That_Contains_The_Same_Transitions()
        {
            var target = CreateTarget();
            var control = CreateControl(target.Object);

            control.Transitions = new Transitions { target.Object };
        }

        [Fact]
        public void Transitions_Can_Re_Set_During_Batch_Update()
        {
            var target = CreateTarget();
            var control = CreateControl(target.Object);

            // Assigning and then clearing Transitions ensures we have a transition state
            // collection created.
            control.Transitions = null;

            control.BeginBatchUpdate();

            // Setting opacity then Transitions means that we receive the Transitions change
            // after the Opacity change when EndBatchUpdate is called.
            control.Opacity = 0.5;
            control.Transitions = new Transitions { target.Object };

            // Which means that the transition state hasn't been initialized with the new
            // Transitions when the Opacity change notification gets raised here.
            control.EndBatchUpdate();
        }

        private static IDisposable Start()
        {
            var clock = new MockGlobalClock();
            var services = TestServices.RealStyler.With(globalClock: clock);
            return UnitTestApplication.Start(services);
        }

        private static Mock<ITransition> CreateTarget()
        {
            return CreateTransition(Visual.OpacityProperty);
        }

        private static Control CreateControl(ITransition transition)
        {
            var control = new Control
            {
                Transitions = new Transitions { transition },
            };

            var root = new TestRoot(control);
            return control;
        }

        private static Control CreateStyledControl(
            ITransition transition1 = null,
            ITransition transition2 = null)
        {
            transition1 = transition1 ?? CreateTarget().Object;
            transition2 = transition2 ?? CreateTransition(Control.WidthProperty).Object;

            var control = new Control
            {
                Styles =
                {
                    new Style(x => x.OfType<Control>())
                    {
                        Setters =
                        {
                            new Setter
                            {
                                Property = Control.TransitionsProperty,
                                Value = new Transitions { transition1 },
                            }
                        }
                    },
                    new Style(x => x.OfType<Control>().Class("foo"))
                    {
                        Setters =
                        {
                            new Setter
                            {
                                Property = Control.TransitionsProperty,
                                Value = new Transitions { transition2 },
                            }
                        }
                    }
                }
            };

            var root = new TestRoot(control);
            return control;
        }

        private static Mock<ITransition> CreateTransition(AvaloniaProperty property)
        {
            var target = new Mock<ITransition>();
            var sub = new Mock<IDisposable>();

            target.Setup(x => x.Property).Returns(property);
            target.Setup(x => x.Apply(
                It.IsAny<Animatable>(),
                It.IsAny<IClock>(),
                It.IsAny<object>(),
                It.IsAny<object>())).Returns(sub.Object);

            return target;
        }
    }
}
