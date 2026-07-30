using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace IntegrationTestApp.Pages;

public partial class KeyboardPage : UserControl
{
    private int _keyDownCount;

    public KeyboardPage()
    {
        InitializeComponent();

        // Gestures without a modifier are the interesting case: on macOS the key down used to be
        // swallowed by the input context while a text input client was active.
        AddKeyBinding(new KeyGesture(Key.Space), "Space");
        AddKeyBinding(new KeyGesture(Key.A), "A");
        AddKeyBinding(new KeyGesture(Key.G, KeyModifiers.Control), "Ctrl+G");

        // TextBox marks TextInput as handled, so an instance handler would never run.
        KeyDownTextBox.AddHandler(TextInputEvent, KeyDownTextBox_TextInput, handledEventsToo: true);
    }

    private void AddKeyBinding(KeyGesture gesture, string name)
    {
        GestureScope.KeyBindings.Add(new KeyBinding
        {
            Gesture = gesture,
            Command = new DelegateCommand(() => LastKeyBinding.Text = name)
        });
    }

    private void KeyDownTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        // While an input method is composing, the key is masked as Key.ImeProcessed but the
        // physical key and the key symbol keep their real values. The key symbol is bracketed
        // to keep whitespace symbols like the space key's visible and assertable.
        LastKeyDown.Text = $"{e.Key}|{e.PhysicalKey}|[{e.KeySymbol}]";

        // Counts every key down, including the ones flagsChanged raises for modifier keys.
        KeyDownCount.Text = (++_keyDownCount).ToString();
    }

    private void KeyDownTextBox_TextInput(object? sender, TextInputEventArgs e)
    {
        LastTextInput.Text = $"[{e.Text}]";
    }

    private void ResetKeyboard_Click(object? sender, RoutedEventArgs e)
    {
        _keyDownCount = 0;
        KeyDownCount.Text = string.Empty;
        LastKeyBinding.Text = string.Empty;
        LastKeyDown.Text = string.Empty;
        LastTextInput.Text = string.Empty;
        GestureTextBox.Text = string.Empty;
        KeyDownTextBox.Text = string.Empty;
    }
}
