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
            var target = new KeyboardDevice();
            var root = new Mock<IInputRoot>();

            target.ProcessRawEvent(
                new RawKeyEventArgs(
                    target,
                    0,
                    root.Object,
                    RawKeyEventType.KeyDown,
                    Key.A,
                    RawInputModifiers.None,
                    PhysicalKey.A,
                    "a"));

            root.Verify(x => x.RaiseEvent(It.IsAny<KeyEventArgs>()));
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
            var target = new KeyboardDevice();
            var root = new Mock<IInputRoot>();

            target.ProcessRawEvent(
                new RawTextInputEventArgs(
                    target,
                    0,
                    root.Object,
                    "Foo"));

            root.Verify(x => x.RaiseEvent(It.IsAny<TextInputEventArgs>()));
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
    }
}
