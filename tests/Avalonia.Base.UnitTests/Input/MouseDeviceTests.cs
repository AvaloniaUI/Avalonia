using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Input
{
    public class MouseDeviceTests
    {
#pragma warning disable CS0618 // Type or member is obsolete
        [Fact]
        public void Capture_Is_Transferred_To_Parent_When_Control_Removed()
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

            Assert.Same(root, target.Captured);
        }
#pragma warning restore CS0618 // Type or member is obsolete

        [Fact]
        public void GetPosition_Should_Respect_Control_RenderTransform()
        {
            var renderer = new Mock<IRenderer>();

            using (TestApplication(renderer.Object))
            {
                var inputManager = InputManager.Instance;

                var root = new TestRoot
                {
                    MouseDevice = new MouseDevice(),
                    Child = new Border
                    {
                        Background = Brushes.Black,
                        RenderTransform = new TranslateTransform(10, 0),
                    }
                };

                SendMouseMove(inputManager, root, new Point(11, 11));

#pragma warning disable CS0618 // Type or member is obsolete
                var result = root.MouseDevice.GetPosition(root.Child);
#pragma warning restore CS0618 // Type or member is obsolete
                Assert.Equal(new Point(1, 11), result);
            }
        }

        private void SendMouseMove(IInputManager inputManager, TestRoot root, Point p = new Point())
        {
            inputManager.ProcessInput(new RawPointerEventArgs(
                root.MouseDevice,
                0,
                root,
                RawPointerEventType.Move,
                p,
                RawInputModifiers.None));
        }

        private IDisposable TestApplication(IRenderer renderer)
        {
            return UnitTestApplication.Start(
                new TestServices(inputManager: new InputManager()));
        }
    }
}
