using System;
using System.Windows.Input;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using Xunit;
using MouseButton = Avalonia.Input.MouseButton;

namespace Avalonia.Controls.UnitTests
{
    public class ButtonTests : ScopedTestBase
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
            var renderer = new Mock<IHitTester>();
            var pt = new Point(50, 50);
            renderer.Setup(r => r.HitTest(It.IsAny<Point>(), It.IsAny<Visual>(), It.IsAny<Func<Visual, bool>>()))
                .Returns<Point, Visual, Func<Visual, bool>>((p, r, f) =>
                    r.Bounds.Contains(p) ? new Visual[] { r } : new Visual[0]);
            
            using var _ = UnitTestApplication.Start(TestServices.StyledWindow);
            
            var root = new Window() { HitTesterOverride = renderer.Object };
            var target = new Button()
            {
                Width = 100,
                Height = 100,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            root.Content = target;
            root.Show();

            bool clicked = false;

            target.Click += (s, e) => clicked = true;

            RaisePointerEntered(target);
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
            var root = new TestRoot();
            var target = new Button()
            {
                Width = 100,
                Height = 100
            };
            root.Child = target;

            bool clicked = false;

            target.Click += (s, e) => clicked = true;

            RaisePointerEntered(target);
            RaisePointerMove(target, new Point(50,50));
            RaisePointerPressed(target, 1, MouseButton.Left, new Point(50, 50));
            RaisePointerExited(target);

            Assert.Equal(_helper.Captured, target);

            RaisePointerReleased(target, MouseButton.Left, new Point(200, 50));

            Assert.Equal(_helper.Captured, null);

            Assert.False(clicked);
        }

        [Fact]
        public void Button_With_RenderTransform_Raises_Click()
        {
            var renderer = new Mock<IHitTester>();
            var pt = new Point(150, 50);
            renderer.Setup(r => r.HitTest(It.IsAny<Point>(), It.IsAny<Visual>(), It.IsAny<Func<Visual, bool>>()))
                .Returns<Point, Visual, Func<Visual, bool>>((p, r, f) =>
                    r.Bounds.Contains(p.Transform(r.RenderTransform.Value.Invert())) ?
                    new Visual[] { r } : new Visual[0]);
            
            using var _ = UnitTestApplication.Start(TestServices.StyledWindow);
            
            var root = new Window() { HitTesterOverride = renderer.Object };
            var target = new Button()
            {
                Width = 100,
                Height = 100,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                RenderTransform = new TranslateTransform { X = 100, Y = 0 }
            };
            root.Content = target;
            root.Show();

            //actual bounds of button should  be 100,0,100,100 x -> translated 100 pixels
            //so mouse with x=150 coordinates should trigger click
            //button shouldn't count on bounds to calculate pointer is in the over or not, but
            //on avalonia event system, as renderer hit test will properly calculate whether to send
            //mouse over events to button based on rendered bounds
            //note: button also may have not rectangular shape and only renderer hit testing is reliable

            bool clicked = false;

            target.Click += (s, e) => clicked = true;

            RaisePointerEntered(target);
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

        [Fact]
        public void Button_Invokes_CanExecute_When_CommandParameter_Changed()
        {
            var target = new Button();
            var raised = 0;

            target.Click += (s, e) => ++raised;

            target.RaiseEvent(new RoutedEventArgs(AccessKeyHandler.AccessKeyPressedEvent));

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Raises_Click_When_AccessKey_Raised()
        {
            var command = new TestCommand(p => p is bool value && value);
            var target = new Button { Command = command };

            target.CommandParameter = true;
            Assert.True(target.IsEffectivelyEnabled);

            target.CommandParameter = false;
            Assert.False(target.IsEffectivelyEnabled);
        }
        
        [Fact]
        public void Button_Invokes_Doesnt_Execute_When_Button_Disabled()
        {
            var target = new Button();
            var raised = 0;

            target.IsEnabled = false;
            target.Click += (s, e) => ++raised;

            target.RaiseEvent(new RoutedEventArgs(AccessKeyHandler.AccessKeyPressedEvent));

            Assert.Equal(0, raised);
        }
        
        [Fact]
        public void Button_IsDefault_Works()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var raised = 0;
                var target = new Button();
                var window = new Window { Content = target };
                window.Show();
                
                target.Click += (s, e) => ++raised;

                target.IsDefault = false;
                window.RaiseEvent(CreateKeyDownEvent(Key.Enter));
                Assert.Equal(0, raised);
                
                target.IsDefault = true;
                window.RaiseEvent(CreateKeyDownEvent(Key.Enter));
                Assert.Equal(1, raised);

                target.IsDefault = false;
                window.RaiseEvent(CreateKeyDownEvent(Key.Enter));
                Assert.Equal(1, raised);
                
                target.IsDefault = true;
                window.RaiseEvent(CreateKeyDownEvent(Key.Enter));
                Assert.Equal(2, raised);
                
                window.Content = null;
                // To check if handler was raised on the button, when it's detached, we need to pass it as a source manually.
                window.RaiseEvent(CreateKeyDownEvent(Key.Enter, target));
                Assert.Equal(2, raised);
            }
        }
        
        [Fact]
        public void Button_IsCancel_Works()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var raised = 0;
                var target = new Button();
                var window = new Window { Content = target };
                window.Show();
                
                target.Click += (s, e) => ++raised;

                target.IsCancel = false;
                window.RaiseEvent(CreateKeyDownEvent(Key.Escape));
                Assert.Equal(0, raised);
                
                target.IsCancel = true;
                window.RaiseEvent(CreateKeyDownEvent(Key.Escape));
                Assert.Equal(1, raised);

                target.IsCancel = false;
                window.RaiseEvent(CreateKeyDownEvent(Key.Escape));
                Assert.Equal(1, raised);
                
                target.IsCancel = true;
                window.RaiseEvent(CreateKeyDownEvent(Key.Escape));
                Assert.Equal(2, raised);
                
                window.Content = null;
                window.RaiseEvent(CreateKeyDownEvent(Key.Escape, target));
                Assert.Equal(2, raised);
            }
        }

        private KeyEventArgs CreateKeyDownEvent(Key key, Interactive source = null)
        {
            return new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key, Source = source };
        }

        private void RaisePointerPressed(Button button, int clickCount, MouseButton mouseButton, Point position)
        {
            _helper.Down(button, mouseButton, position, clickCount: clickCount);
        }

        private void RaisePointerReleased(Button button, MouseButton mouseButton, Point pt)
        {
            _helper.Up(button, mouseButton, pt);
        }

        private void RaisePointerEntered(Button button)
        {
            _helper.Enter(button);
        }

        private void RaisePointerExited(Button button)
        {
            _helper.Leave(button);
        }

        private void RaisePointerMove(Button button, Point pos)
        {
            _helper.Move(button, pos);
        }

        private class TestCommand : ICommand
        {
            private readonly Func<object, bool> _canExecute;
            private readonly Action<object> _execute;
            private EventHandler _canExecuteChanged;
            private bool _enabled = true;

            public TestCommand(bool enabled = true)
            {
                _enabled = enabled;
                _canExecute = _ => _enabled;
                _execute = _ => { };
            }

            public TestCommand(Func<object, bool> canExecute, Action<object> execute = null)
            {
                _canExecute = canExecute;
                _execute = execute ?? (_ => { });
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

            public bool CanExecute(object parameter) => _canExecute(parameter);

            public void Execute(object parameter) => _execute(parameter);
        }
    }
}
