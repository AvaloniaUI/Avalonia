using System;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Animation.UnitTests
{
    public class AnimatableTests
    {
        [Fact]
        public void Transition_Is_Not_Applied_When_Not_Attached_To_Visual_Tree()
        {
            var target = CreateTarget();
            var control = new Control
            {
                Transitions = { target.Object },
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
            using (UnitTestApplication.Start(TestServices.RealStyler))
            {
                var target = CreateTarget();
                var control = new Control
                {
                    Transitions = { target.Object },
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

        [Fact]
        public void Transition_Is_Not_Applied_When_StyleTrigger_Changes_With_LocalValue_Present()
        {
            var target = CreateTarget();
            var control = CreateControl(target.Object);

            control.SetValue(Visual.OpacityProperty, 0.5);

            target.Verify(x => x.Apply(
                control,
                It.IsAny<IClock>(),
                1.0,
                0.5));
            target.ResetCalls();

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
            var target = CreateTarget();
            var control = CreateControl(target.Object);
            var sub = new Mock<IDisposable>();

            target.Setup(x => x.Apply(control, It.IsAny<IClock>(), 1.0, 0.5)).Returns(sub.Object);

            control.Opacity = 0.5;
            sub.ResetCalls();
            control.Opacity = 0.4;

            sub.Verify(x => x.Dispose());
        }

        [Fact]
        public void New_Transition_Is_Applied_When_Local_Value_Changes()
        {
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
            target.ResetCalls();

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
            var target = CreateTarget();
            var control = CreateControl(target.Object);

            control.Opacity = 0.5;

            target.Verify(x => x.Apply(
                control,
                It.IsAny<IClock>(),
                1.0,
                0.5));
            target.ResetCalls();

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

        private static Mock<ITransition> CreateTarget()
        {
            var target = new Mock<ITransition>();
            var sub = new Mock<IDisposable>();

            target.Setup(x => x.Property).Returns(Visual.OpacityProperty);
            target.Setup(x => x.Apply(
                It.IsAny<Animatable>(),
                It.IsAny<IClock>(),
                It.IsAny<object>(),
                It.IsAny<object>())).Returns(sub.Object);

            return target;
        }

        private static Control CreateControl(ITransition transition)
        {
            var control = new Control
            {
                Transitions = { transition },
            };

            var root = new TestRoot(control);
            return control;
        }
    }
}
