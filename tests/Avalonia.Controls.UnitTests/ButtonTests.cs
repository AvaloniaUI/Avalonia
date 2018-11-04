using System;
using System.Windows.Input;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ButtonTests
    {
        [Fact]
        public void Button_Is_Disabled_When_Command_Is_Disabled()
        {
            var command = new TestCommand(false);
            var target = new Button
            {
                Command = command,
            };

            Assert.False(target.IsEnabled);
            command.IsEnabled = true;
            Assert.True(target.IsEnabled);
            command.IsEnabled = false;
            Assert.False(target.IsEnabled);
        }

        [Fact]
        public void Button_Is_Disabled_When_Bound_Command_Doesnt_Exist()
        {
            var target = new Button
            {
                [!Button.CommandProperty] = new Binding("Command"),
            };

            Assert.False(target.IsEnabled);
        }

        [Fact]
        public void Button_Is_Disabled_When_Bound_Command_Is_Removed()
        {
            var viewModel = new
            {
                Command = new TestCommand(true),
            };

            var target = new Button
            {
                DataContext = viewModel,
                [!Button.CommandProperty] = new Binding("Command"),
            };

            Assert.True(target.IsEnabled);
            target.DataContext = null;
            Assert.False(target.IsEnabled);
        }

        [Fact]
        public void Button_Is_Enabled_When_Bound_Command_Is_Added()
        {
            var viewModel = new
            {
                Command = new TestCommand(true),
            };

            var target = new Button
            {
                DataContext = new object(),
                [!Button.CommandProperty] = new Binding("Command"),
            };

            Assert.False(target.IsEnabled);
            target.DataContext = viewModel;
            Assert.True(target.IsEnabled);
        }

        [Fact]
        public void Button_Is_Disabled_When_Disabled_Bound_Command_Is_Added()
        {
            var viewModel = new
            {
                Command = new TestCommand(false),
            };

            var target = new Button
            {
                DataContext = new object(),
                [!Button.CommandProperty] = new Binding("Command"),
            };

            Assert.False(target.IsEnabled);
            target.DataContext = viewModel;
            Assert.False(target.IsEnabled);
        }

        [Fact]
        public void Button_Raises_Click()
        {
            var mouse = Mock.Of<IMouseDevice>();
            var renderer = Mock.Of<IRenderer>();
            IInputElement captured = null;
            Mock.Get(mouse).Setup(m => m.GetPosition(It.IsAny<IVisual>())).Returns(new Point(50, 50));
            Mock.Get(mouse).Setup(m => m.Capture(It.IsAny<IInputElement>())).Callback<IInputElement>(v => captured = v);
            Mock.Get(mouse).Setup(m => m.Captured).Returns(() => captured);
            Mock.Get(renderer).Setup(r => r.HitTest(It.IsAny<Point>(), It.IsAny<IVisual>(), It.IsAny<Func<IVisual, bool>>()))
                .Returns<Point, IVisual, Func<IVisual, bool>>((p, r, f) =>
                    r.Bounds.Contains(p) ? new IVisual[] { r } : new IVisual[0]);

            var target = new TestButton()
            {
                Bounds = new Rect(0, 0, 100, 100),
                Renderer = renderer
            };

            bool clicked = false;

            target.Click += (s, e) => clicked = true;

            RaisePointerEnter(target, mouse);
            RaisePointerMove(target, mouse);
            RaisePointerPressed(target, mouse, 1, MouseButton.Left);

            Assert.Equal(captured, target);

            RaisePointerReleased(target, mouse, MouseButton.Left);

            Assert.Equal(captured, null);

            Assert.True(clicked);
        }

        [Fact]
        public void Button_Does_Not_Raise_Click_When_PointerReleased_Outside()
        {
            var mouse = Mock.Of<IMouseDevice>();
            var renderer = Mock.Of<IRenderer>();
            IInputElement captured = null;
            Mock.Get(mouse).Setup(m => m.GetPosition(It.IsAny<IVisual>())).Returns(new Point(200, 50));
            Mock.Get(mouse).Setup(m => m.Capture(It.IsAny<IInputElement>())).Callback<IInputElement>(v => captured = v);
            Mock.Get(mouse).Setup(m => m.Captured).Returns(() => captured);
            Mock.Get(renderer).Setup(r => r.HitTest(It.IsAny<Point>(), It.IsAny<IVisual>(), It.IsAny<Func<IVisual, bool>>()))
                .Returns<Point, IVisual, Func<IVisual, bool>>((p, r, f) =>
                    r.Bounds.Contains(p) ? new IVisual[] { r } : new IVisual[0]);

            var target = new TestButton()
            {
                Bounds = new Rect(0, 0, 100, 100),
                Renderer = renderer
            };

            bool clicked = false;

            target.Click += (s, e) => clicked = true;

            RaisePointerEnter(target, mouse);
            RaisePointerMove(target, mouse);
            RaisePointerPressed(target, mouse, 1, MouseButton.Left);
            RaisePointerLeave(target, mouse);

            Assert.Equal(captured, target);

            RaisePointerReleased(target, mouse, MouseButton.Left);

            Assert.Equal(captured, null);

            Assert.False(clicked);
        }

        [Fact]
        public void Button_With_RenderTransform_Raises_Click()
        {
            var mouse = Mock.Of<IMouseDevice>();
            var renderer = Mock.Of<IRenderer>();
            IInputElement captured = null;
            Mock.Get(mouse).Setup(m => m.GetPosition(It.IsAny<IVisual>())).Returns(new Point(150, 50));
            Mock.Get(mouse).Setup(m => m.Capture(It.IsAny<IInputElement>())).Callback<IInputElement>(v => captured = v);
            Mock.Get(mouse).Setup(m => m.Captured).Returns(() => captured);
            Mock.Get(renderer).Setup(r => r.HitTest(It.IsAny<Point>(), It.IsAny<IVisual>(), It.IsAny<Func<IVisual, bool>>()))
                .Returns<Point, IVisual, Func<IVisual, bool>>((p, r, f) =>
                    r.Bounds.Contains(p.Transform(r.RenderTransform.Value.Invert())) ?
                    new IVisual[] { r } : new IVisual[0]);

            var target = new TestButton()
            {
                Bounds = new Rect(0, 0, 100, 100),
                RenderTransform = new TranslateTransform { X = 100, Y = 0 },
                Renderer = renderer
            };

            //actual bounds of button should  be 100,0,100,100 x -> translated 100 pixels
            //so mouse with x=150 coordinates should trigger click
            //button shouldn't count on bounds to calculate pointer is in the over or not, but
            //on avalonia event system, as renderer hit test will properly calculate whether to send
            //mouse over events to button based on rendered bounds
            //note: button also may have not rectangular shape and only renderer hit testing is reliable

            bool clicked = false;

            target.Click += (s, e) => clicked = true;

            RaisePointerEnter(target, mouse);
            RaisePointerMove(target, mouse);
            RaisePointerPressed(target, mouse, 1, MouseButton.Left);

            Assert.Equal(captured, target);

            RaisePointerReleased(target, mouse, MouseButton.Left);

            Assert.Equal(captured, null);

            Assert.True(clicked);
        }

        private class TestButton : Button, IRenderRoot
        {
            public TestButton()
            {
                IsVisible = true;
            }

            public new Rect Bounds
            {
                get => base.Bounds;
                set => base.Bounds = value;
            }

            public Size ClientSize => throw new NotImplementedException();

            public IRenderer Renderer { get; set; }

            public double RenderScaling => throw new NotImplementedException();

            public IRenderTarget CreateRenderTarget() => throw new NotImplementedException();

            public void Invalidate(Rect rect) => throw new NotImplementedException();

            public Point PointToClient(Point point) => throw new NotImplementedException();

            public Point PointToScreen(Point point) => throw new NotImplementedException();
        }

        private void RaisePointerPressed(Button button, IMouseDevice device, int clickCount, MouseButton mouseButton)
        {
            button.RaiseEvent(new PointerPressedEventArgs
            {
                RoutedEvent = InputElement.PointerPressedEvent,
                Source = button,
                MouseButton = mouseButton,
                ClickCount = clickCount,
                Device = device,
            });
        }

        private void RaisePointerReleased(Button button, IMouseDevice device, MouseButton mouseButton)
        {
            button.RaiseEvent(new PointerReleasedEventArgs
            {
                RoutedEvent = InputElement.PointerReleasedEvent,
                Source = button,
                MouseButton = mouseButton,
                Device = device,
            });
        }

        private void RaisePointerEnter(Button button, IMouseDevice device)
        {
            button.RaiseEvent(new PointerEventArgs
            {
                RoutedEvent = InputElement.PointerEnterEvent,
                Source = button,
                Device = device,
            });
        }

        private void RaisePointerLeave(Button button, IMouseDevice device)
        {
            button.RaiseEvent(new PointerEventArgs
            {
                RoutedEvent = InputElement.PointerLeaveEvent,
                Source = button,
                Device = device,
            });
        }

        private void RaisePointerMove(Button button, IMouseDevice device)
        {
            button.RaiseEvent(new PointerEventArgs
            {
                RoutedEvent = InputElement.PointerMovedEvent,
                Source = button,
                Device = device,
            });
        }

        private class TestCommand : ICommand
        {
            private bool _enabled;

            public TestCommand(bool enabled)
            {
                _enabled = enabled;
            }

            public bool IsEnabled
            {
                get { return _enabled; }
                set
                {
                    if (_enabled != value)
                    {
                        _enabled = value;
                        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
            }

            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter) => _enabled;

            public void Execute(object parameter)
            {
            }
        }
    }
}
