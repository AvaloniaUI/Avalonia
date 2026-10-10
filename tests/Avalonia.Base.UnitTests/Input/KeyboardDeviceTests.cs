using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Input
{
    public class KeyboardDeviceTests
    {
        [Fact]
        public void Keypresses_Should_Be_Sent_To_Root_If_No_Focused_Element()
        {
            using (UnitTestApplication.Start(TestServices.FocusableWindow))
            {
                var window = new Window();
                window.FocusManager.Focus(null);
                int raised = 0;
                window.KeyDown += (sender, ev) =>
                {
                    if (sender == window && ev.RoutedEvent == InputElement.KeyDownEvent)
                        raised++;
                };
                KeyboardDevice.Instance!.ProcessRawEvent(
                    new RawKeyEventArgs(
                        KeyboardDevice.Instance,
                        0,
                        window.GetInputRoot()!,
                        RawKeyEventType.KeyDown,
                        Key.A,
                        RawInputModifiers.None,
                        PhysicalKey.A,
                        "a"));
                Assert.Equal(1, raised);
            }
        }

        [Fact]
        public void Keypresses_Should_Be_Sent_To_Focused_Element()
        {
            var target = new KeyboardDevice();
            var focused = new Control();
            var root = new TestRoot();
            var raised = 0;

            target.SetFocusedElement(
                focused,
                NavigationMethod.Unspecified,
                KeyModifiers.None);

            focused.KeyDown += (s, e) => ++raised;

            target.ProcessRawEvent(
                new RawKeyEventArgs(
                    target,
                    0,
                    root,
                    RawKeyEventType.KeyDown,
                    Key.A,
                    RawInputModifiers.None,
                    PhysicalKey.A,
                    "a"));

            Assert.Equal(1, raised);
        }

        [Fact]
        public void TextInput_Should_Be_Sent_To_Root_If_No_Focused_Element()
        {
            using (UnitTestApplication.Start(TestServices.FocusableWindow))
            {
                var window = new Window();
                window.FocusManager.Focus(null);
                int raised = 0;
                window.TextInput += (sender, ev) =>
                {
                    if (sender == window && ev.RoutedEvent == InputElement.TextInputEvent)
                        raised++;
                };
                KeyboardDevice.Instance!.ProcessRawEvent(
                    new RawTextInputEventArgs(
                        KeyboardDevice.Instance,
                        0,
                        window.GetInputRoot()!,
                        "Foo"));
                Assert.Equal(1, raised);
            }
        }

        [Fact]
        public void TextInput_Should_Be_Sent_To_Focused_Element()
        {
            var target = new KeyboardDevice();
            var focused = new Control();
            var root = new TestRoot();
            var raised = 0;

            target.SetFocusedElement(
                focused,
                NavigationMethod.Unspecified,
                KeyModifiers.None);

            focused.TextInput += (s, e) => ++raised;

            target.ProcessRawEvent(
                new RawTextInputEventArgs(
                    target,
                    0,
                    root,
                    "Foo"));

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Can_Change_KeyBindings_In_Keybinding_Event_Handler()
        {
            var target = new KeyboardDevice();
            var button = new Button();
            var root = new TestRoot(button);
            var raised = 0;

            button.KeyBindings.Add(new KeyBinding
            {
                Gesture = new KeyGesture(Key.O, KeyModifiers.Control),
                Command = new Utilities.DelegateCommand(() =>
                {
                    button.KeyBindings.Clear();
                    ++raised;
                }),
            });

            target.SetFocusedElement(button, NavigationMethod.Pointer, 0);
            target.ProcessRawEvent(
                new RawKeyEventArgs(
                    target,
                    0,
                    root,
                    RawKeyEventType.KeyDown,
                    Key.O,
                    RawInputModifiers.Control,
                    PhysicalKey.O,
                    "o"));

            Assert.Equal(1, raised);
        }

        private class HandlingControl : Control
        {
            public int Handled { get; private set; }

            protected override void OnKeyDown(KeyEventArgs e)
            {
                if (e.Key == Key.V && e.KeyModifiers == KeyModifiers.Control)
                {
                    ++Handled;
                    e.Handled = true;
                }
                base.OnKeyDown(e);
            }
        }

        private static RawKeyEventArgs CtrlV(KeyboardDevice device, IInputRoot root) =>
            new RawKeyEventArgs(device, 0, root, RawKeyEventType.KeyDown, Key.V,
                RawInputModifiers.Control, PhysicalKey.V, "v");

        [Fact]
        public void Focused_Control_Should_Handle_Key_Before_Ancestor_KeyBinding()
        {
            var target = new KeyboardDevice();
            var focused = new HandlingControl();
            var parent = new Panel { Children = { focused } };
            var root = new TestRoot(parent);
            var raised = 0;

            parent.KeyBindings.Add(new KeyBinding
            {
                Gesture = new KeyGesture(Key.V, KeyModifiers.Control),
                Command = new Utilities.DelegateCommand(() => ++raised),
            });

            target.SetFocusedElement(focused, NavigationMethod.Pointer, 0);
            target.ProcessRawEvent(CtrlV(target, root));

            Assert.Equal(1, focused.Handled);
            Assert.Equal(0, raised);
        }

        [Fact]
        public void KeyBinding_On_Focused_Control_Should_Take_Priority_Over_Its_OnKeyDown()
        {
            var target = new KeyboardDevice();
            var focused = new HandlingControl();
            var root = new TestRoot(focused);
            var raised = 0;

            focused.KeyBindings.Add(new KeyBinding
            {
                Gesture = new KeyGesture(Key.V, KeyModifiers.Control),
                Command = new Utilities.DelegateCommand(() => ++raised),
            });

            target.SetFocusedElement(focused, NavigationMethod.Pointer, 0);
            target.ProcessRawEvent(CtrlV(target, root));

            Assert.Equal(0, focused.Handled);
            Assert.Equal(1, raised);
        }

        [Fact]
        public void Inner_KeyBinding_Should_Take_Priority_Over_Outer_KeyBinding()
        {
            var target = new KeyboardDevice();
            var focused = new Control();
            var parent = new Panel { Children = { focused } };
            var root = new TestRoot(parent);
            var inner = 0;
            var outer = 0;

            parent.KeyBindings.Add(new KeyBinding
            {
                Gesture = new KeyGesture(Key.V, KeyModifiers.Control),
                Command = new Utilities.DelegateCommand(() => ++inner),
            });
            root.KeyBindings.Add(new KeyBinding
            {
                Gesture = new KeyGesture(Key.V, KeyModifiers.Control),
                Command = new Utilities.DelegateCommand(() => ++outer),
            });

            target.SetFocusedElement(focused, NavigationMethod.Pointer, 0);
            target.ProcessRawEvent(CtrlV(target, root));

            Assert.Equal(1, inner);
            Assert.Equal(0, outer);
        }

        [Fact]
        public void Control_Focus_Should_Be_Set_Before_FocusedElement_Raises_PropertyChanged()
        {
            var target = new KeyboardDevice();
            var focused = new Control();
            var root = new TestRoot();
            var gotFocusRaised = 0;
            var propertyChangedRaised = 0;

            focused.GotFocus += (s, e) => ++gotFocusRaised;

            target.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(target.FocusedElement))
                {
                    Assert.Equal(1, gotFocusRaised);
                    ++propertyChangedRaised;
                }
            };

            target.SetFocusedElement(
                focused,
                NavigationMethod.Unspecified,
                KeyModifiers.None);

            Assert.Equal(1, propertyChangedRaised);
        }

        [Fact]
        public void Cancelled_Focus_Change_Should_Not_Send_Got_Focus_Event()
        {
            var target = new KeyboardDevice();
            var focused = new Control();
            var root = new TestRoot();
            bool focusCancelled = false;

            focused.GettingFocus += (s, e) =>
            {
                focusCancelled = e.TryCancel();
            };

            focused.GotFocus += (s, e) =>
            {
                focusCancelled = false;
            };

            target.SetFocusedElement(
                focused,
                NavigationMethod.Unspecified,
                KeyModifiers.None);

            Assert.True(focusCancelled);
        }

        [Fact]
        public void Redirected_Focus_Should_Change_Focused_Element()
        {
            var target = new KeyboardDevice();
            var first = new Control();
            var second = new Control();
            var stack = new StackPanel();
            stack.Children.AddRange(new[] { first, second });
            var root = new TestRoot(stack);

            first.GettingFocus += (s, e) =>
            {
                e.TrySetNewFocusedElement(second);
            };

            target.SetFocusedElement(
                first,
                NavigationMethod.Unspecified,
                KeyModifiers.None);

            Assert.True(second.IsFocused);
        }
    }
}
