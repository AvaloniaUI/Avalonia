using System;
using System.Windows.Input;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ButtonTests
    {
        private MouseTestHelper _helper = new MouseTestHelper();
        
        [Fact]
        public void Button_Is_Disabled_When_Command_Is_Disabled()
        {
            var command = new TestCommand(false);
            var target = new Button
            {
                Command = command,
            };
            var root = new TestRoot { Child = target };

            Assert.False(target.IsEffectivelyEnabled);
            command.IsEnabled = true;
            Assert.True(target.IsEffectivelyEnabled);
            command.IsEnabled = false;
            Assert.False(target.IsEffectivelyEnabled);
        }

        [Fact]
        public void Button_Is_Disabled_When_Command_Is_Enabled_But_IsEnabled_Is_False()
        {
            var command = new TestCommand(true);
            var target = new Button
            {
                IsEnabled = false,
                Command = command,
            };

            var root = new TestRoot { Child = target };

            Assert.False(((IInputElement)target).IsEffectivelyEnabled);
        }

        [Fact]
        public void Button_Is_Disabled_When_Bound_Command_Doesnt_Exist()
        {
            var target = new Button
            {
                [!Button.CommandProperty] = new Binding("Command"),
            };

            Assert.True(target.IsEnabled);
            Assert.False(target.IsEffectivelyEnabled);
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
            Assert.True(target.IsEffectivelyEnabled);

            target.DataContext = null;

            Assert.True(target.IsEnabled);
            Assert.False(target.IsEffectivelyEnabled);
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

            Assert.True(target.IsEnabled);
            Assert.False(target.IsEffectivelyEnabled);

            target.DataContext = viewModel;

            Assert.True(target.IsEnabled);
            Assert.True(target.IsEffectivelyEnabled);
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

            Assert.True(target.IsEnabled);
            Assert.False(target.IsEffectivelyEnabled);

            target.DataContext = viewModel;

            Assert.True(target.IsEnabled);
            Assert.False(target.IsEffectivelyEnabled);
        }

        [Fact]
        public void Button_Raises_Click()
        {
            var renderer = Mock.Of<IRenderer>();
            var pt = new Point(50, 50);
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

            RaisePointerEnter(target);
            RaisePointerMove(target, pt);
            RaisePointerPressed(target, 1, MouseButton.Left, pt);

            Assert.Equal(_helper.Captured, target);

            RaisePointerReleased(target, MouseButton.Left, pt);

            Assert.Equal(_helper.Captured, null);

            Assert.True(clicked);
        }

        [Fact]
        public void Button_Does_Not_Raise_Click_When_PointerReleased_Outside()
        {
            var renderer = Mock.Of<IRenderer>();
            
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

            RaisePointerEnter(target);
            RaisePointerMove(target, new Point(50,50));
            RaisePointerPressed(target, 1, MouseButton.Left, new Point(50, 50));
            RaisePointerLeave(target);

            Assert.Equal(_helper.Captured, target);

            RaisePointerReleased(target, MouseButton.Left, new Point(200, 50));

            Assert.Equal(_helper.Captured, null);

            Assert.False(clicked);
        }

        [Fact]
        public void Button_With_RenderTransform_Raises_Click()
        {
            var renderer = Mock.Of<IRenderer>();
            var pt = new Point(150, 50);
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

            RaisePointerEnter(target);
            RaisePointerMove(target, pt);
            RaisePointerPressed(target, 1, MouseButton.Left, pt);

            Assert.Equal(_helper.Captured, target);

            RaisePointerReleased(target, MouseButton.Left, pt);

            Assert.Equal(_helper.Captured, null);

            Assert.True(clicked);
        }

        [Fact]
        public void Button_Does_Not_Subscribe_To_Command_CanExecuteChanged_Until_Added_To_Logical_Tree()
        {
            var command = new TestCommand(true);
            var target = new Button
            {
                Command = command,
            };

            Assert.Equal(0, command.SubscriptionCount);
        }

        [Fact]
        public void Button_Subscribes_To_Command_CanExecuteChanged_When_Added_To_Logical_Tree()
        {
            var command = new TestCommand(true);
            var target = new Button { Command = command };
            var root = new TestRoot { Child = target };

            Assert.Equal(1, command.SubscriptionCount);
        }

        [Fact]
        public void Button_Unsubscribes_From_Command_CanExecuteChanged_When_Removed_From_Logical_Tree()
        {
            var command = new TestCommand(true);
            var target = new Button { Command = command };
            var root = new TestRoot { Child = target };

            root.Child = null;
            Assert.Equal(0, command.SubscriptionCount);
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

            public Point PointToClient(PixelPoint p) => throw new NotImplementedException();

            public PixelPoint PointToScreen(Point p) => throw new NotImplementedException();
        }

        private void RaisePointerPressed(Button button, int clickCount, MouseButton mouseButton, Point position)
        {
            _helper.Down(button, mouseButton, position, clickCount: clickCount);
        }

        private void RaisePointerReleased(Button button, MouseButton mouseButton, Point pt)
        {
            _helper.Up(button, mouseButton, pt);
        }

        private void RaisePointerEnter(Button button)
        {
            _helper.Enter(button);
        }

        private void RaisePointerLeave(Button button)
        {
            _helper.Leave(button);
        }

        private void RaisePointerMove(Button button, Point pos)
        {
            _helper.Move(button, pos);
        }

        private class TestCommand : ICommand
        {
            private EventHandler _canExecuteChanged;
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
                        _canExecuteChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
            }

            public int SubscriptionCount { get; private set; }

            public event EventHandler CanExecuteChanged
            {
                add { _canExecuteChanged += value; ++SubscriptionCount; }
                remove { _canExecuteChanged -= value; --SubscriptionCount; }
            }

            public bool CanExecute(object parameter) => _enabled;

            public void Execute(object parameter)
            {
            }
        }
    }
}
