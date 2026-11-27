using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Input
{
    public class KeyboardDeviceTests : ScopedTestBase
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

        [Fact]
        public void Focus_Moved_Back_From_GotFocus_Handler_Should_Keep_Text_Input_Client()
        {
            var target = new KeyboardDevice();
            var textInput = new Control();
            var popupItem = new Control();
            var stack = new StackPanel();
            stack.Children.AddRange(new[] { textInput, popupItem });
            var inputMethod = new RecordingInputMethod();
            var root = new TestRoot(stack) { InputMethod = inputMethod };
            var client = new TestTextInputMethodClient(textInput);
            textInput.TextInputMethodClientRequested += (_, e) => e.Client = client;

            target.SetFocusedElement(textInput, NavigationMethod.Unspecified, KeyModifiers.None);

            // Like a completion list that hands focus back to its text box when an item is picked.
            popupItem.GotFocus += (_, _) =>
                target.SetFocusedElement(textInput, NavigationMethod.Unspecified, KeyModifiers.None);

            target.SetFocusedElement(popupItem, NavigationMethod.Pointer, KeyModifiers.None);

            Assert.Same(textInput, target.FocusedElement);
            Assert.Same(client, inputMethod.Client);
        }

        [Fact]
        public void Focus_Moved_Back_From_LostFocus_Handler_Should_Keep_Text_Input_Client()
        {
            var target = new KeyboardDevice();
            var textInput = new Control();
            var other = new Control();
            var stack = new StackPanel();
            stack.Children.AddRange(new[] { textInput, other });
            var inputMethod = new RecordingInputMethod();
            var root = new TestRoot(stack) { InputMethod = inputMethod };
            var client = new TestTextInputMethodClient(textInput);
            textInput.TextInputMethodClientRequested += (_, e) => e.Client = client;
            var otherGotFocus = 0;
            other.GotFocus += (_, _) => ++otherGotFocus;

            target.SetFocusedElement(textInput, NavigationMethod.Unspecified, KeyModifiers.None);

            var refocused = false;
            textInput.LostFocus += (_, _) =>
            {
                if (refocused)
                    return;
                refocused = true;
                target.SetFocusedElement(textInput, NavigationMethod.Unspecified, KeyModifiers.None);
            };

            target.SetFocusedElement(other, NavigationMethod.Pointer, KeyModifiers.None);

            Assert.Same(textInput, target.FocusedElement);
            Assert.Equal(0, otherGotFocus);
            Assert.Same(client, inputMethod.Client);
        }

        private class RecordingInputMethod : ITextInputMethodImpl
        {
            public TextInputMethodClient? Client { get; private set; }
            public void SetClient(TextInputMethodClient? client) => Client = client;
            public void SetCursorRect(Rect rect) { }
            public void SetOptions(TextInputOptions options) { }
            public void Reset() { }
        }

        private class TestTextInputMethodClient(Visual textViewVisual) : TextInputMethodClient
        {
            public override Visual TextViewVisual => textViewVisual;
            public override bool SupportsPreedit => false;
            public override bool SupportsSurroundingText => false;
            public override string SurroundingText => "";
            public override Rect CursorRectangle => default;
            public override TextSelection Selection { get; set; }
        }
    }
}
