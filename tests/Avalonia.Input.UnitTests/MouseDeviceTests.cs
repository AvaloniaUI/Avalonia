using Avalonia.Controls;
using Avalonia.Input.Raw;
using Avalonia.Rendering;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using System;
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
        public void MouseMove_Should_Update_PointerOver()
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

                renderer.Setup(x => x.HitTest(It.IsAny<Point>(), It.IsAny<IVisual>(), It.IsAny<Func<IVisual, bool>>()))
                    .Returns(new[] { decorator });

                inputManager.ProcessInput(new RawMouseEventArgs(
                    root.MouseDevice,
                    0,
                    root,
                    RawMouseEventType.Move,
                    new Point(),
                    InputModifiers.None));

                Assert.True(decorator.IsPointerOver);
                Assert.True(border.IsPointerOver);
                Assert.False(canvas.IsPointerOver);
                Assert.True(root.IsPointerOver);

                renderer.Setup(x => x.HitTest(It.IsAny<Point>(), It.IsAny<IVisual>(), It.IsAny<Func<IVisual, bool>>()))
                    .Returns(new[] { canvas });

                inputManager.ProcessInput(new RawMouseEventArgs(
                    root.MouseDevice,
                    0,
                    root,
                    RawMouseEventType.Move,
                    new Point(),
                    InputModifiers.None));

                Assert.False(decorator.IsPointerOver);
                Assert.False(border.IsPointerOver);
                Assert.True(canvas.IsPointerOver);
                Assert.True(root.IsPointerOver);
            }
        }

        private IDisposable TestApplication(IRenderer renderer)
        {
            return UnitTestApplication.Start(
                new TestServices(inputManager: new InputManager()));
        }
    }
}
