using Avalonia.Controls;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Rendering;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace Avalonia.Input.UnitTests
{
    public class MouseDeviceTests
    {
        [Fact]
        public void Capture_Is_Cleared_When_Control_Removed()
        {
            Canvas control;
            var root = new TestRoot
            {
                Child = control = new Canvas(),
            };
            var target = new MouseDevice();

            target.Capture(control);
            Assert.Same(control, target.Captured);

            root.Child = null;

            Assert.Null(target.Captured);
        }

        [Fact]
        public void MouseMove_Should_Update_IsPointerOver()
        {
            var renderer = new Mock<IRenderer>();

            using (TestApplication(renderer.Object))
            {
                var inputManager = InputManager.Instance;

                Canvas canvas;
                Border border;
                Decorator decorator;

                var root = new TestRoot
                {
                    MouseDevice = new MouseDevice(),
                    Renderer = renderer.Object,
                    Child = new Panel
                    {
                        Children =
                        {
                            (canvas = new Canvas()),
                            (border = new Border
                            {
                                Child = decorator = new Decorator(),
                            })
                        }
                    }
                };

                SetHit(renderer, decorator);
                SendMouseMove(inputManager, root);

                Assert.True(decorator.IsPointerOver);
                Assert.True(border.IsPointerOver);
                Assert.False(canvas.IsPointerOver);
                Assert.True(root.IsPointerOver);

                SetHit(renderer, canvas);
                SendMouseMove(inputManager, root);

                Assert.False(decorator.IsPointerOver);
                Assert.False(border.IsPointerOver);
                Assert.True(canvas.IsPointerOver);
                Assert.True(root.IsPointerOver);
            }
        }

        [Fact]
        public void IsPointerOver_Should_Be_Updated_When_Child_Sets_Handled_True()
        {
            var renderer = new Mock<IRenderer>();

            using (TestApplication(renderer.Object))
            {
                var inputManager = InputManager.Instance;

                Canvas canvas;
                Border border;
                Decorator decorator;

                var root = new TestRoot
                {
                    MouseDevice = new MouseDevice(),
                    Renderer = renderer.Object,
                    Child = new Panel
                    {
                        Children =
                        {
                            (canvas = new Canvas()),
                            (border = new Border
                            {
                                Child = decorator = new Decorator(),
                            })
                        }
                    }
                };

                SetHit(renderer, canvas);
                SendMouseMove(inputManager, root);

                Assert.False(decorator.IsPointerOver);
                Assert.False(border.IsPointerOver);
                Assert.True(canvas.IsPointerOver);
                Assert.True(root.IsPointerOver);

                // Ensure that e.Handled is reset between controls.
                decorator.PointerEnter += (s, e) => e.Handled = true;

                SetHit(renderer, decorator);
                SendMouseMove(inputManager, root);

                Assert.True(decorator.IsPointerOver);
                Assert.True(border.IsPointerOver);
                Assert.False(canvas.IsPointerOver);
                Assert.True(root.IsPointerOver);
            }
        }

        [Fact]
        public void PointerEnter_Leave_Should_Be_Raised_In_Correct_Order()
        {
            var renderer = new Mock<IRenderer>();
            var result = new List<(object, string)>();

            void HandleEvent(object sender, PointerEventArgs e)
            {
                result.Add((sender, e.RoutedEvent.Name));
            }

            using (TestApplication(renderer.Object))
            {
                var inputManager = InputManager.Instance;

                Canvas canvas;
                Border border;
                Decorator decorator;

                var root = new TestRoot
                {
                    MouseDevice = new MouseDevice(),
                    Renderer = renderer.Object,
                    Child = new Panel
                    {
                        Children =
                        {
                            (canvas = new Canvas()),
                            (border = new Border
                            {
                                Child = decorator = new Decorator(),
                            })
                        }
                    }
                };

                SetHit(renderer, canvas);
                SendMouseMove(inputManager, root);

                AddEnterLeaveHandlers(HandleEvent, root, canvas, border, decorator);
                SetHit(renderer, decorator);
                SendMouseMove(inputManager, root);

                Assert.Equal(
                    new[]
                    {
                        ((object)canvas, "PointerLeave"),
                        ((object)decorator, "PointerEnter"),
                        ((object)border, "PointerEnter"),
                    },
                    result);
            }
        }

        private void AddEnterLeaveHandlers(
            EventHandler<PointerEventArgs> handler,
            params IControl[] controls)
        {
            foreach (var c in controls)
            {
                c.PointerEnter += handler;
                c.PointerLeave += handler;
            }
        }

        private void SendMouseMove(IInputManager inputManager, TestRoot root)
        {
            inputManager.ProcessInput(new RawMouseEventArgs(
                root.MouseDevice,
                0,
                root,
                RawMouseEventType.Move,
                new Point(),
                InputModifiers.None));
        }

        private void SetHit(Mock<IRenderer> renderer, IControl hit)
        {
            renderer.Setup(x => x.HitTest(It.IsAny<Point>(), It.IsAny<IVisual>(), It.IsAny<Func<IVisual, bool>>()))
                .Returns(new[] { hit });
        }

        private IDisposable TestApplication(IRenderer renderer)
        {
            return UnitTestApplication.Start(
                new TestServices(inputManager: new InputManager()));
        }
    }
}
