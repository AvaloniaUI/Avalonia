using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Logging;
using Avalonia.Tizen.Platform.Input;
using Tizen.NUI;
using Key = Tizen.NUI.Key;

namespace Avalonia.Tizen;
internal class NuiKeyboardHandler
{
    private const string LogKey = "TIZENHKEY";

    private readonly NuiAvaloniaView _view;

    public NuiKeyboardHandler(NuiAvaloniaView view)
    {
        _view = view;
    }

    public void Handle(Window.KeyEventArgs e)
    {
        Logger.TryGet(LogEventLevel.Debug, LogKey)?.Log(null, "Key fired {text}", e.Key.KeyPressedName);

        if (_view.TextEditor.IsActive)
            return;

        if (ShouldSendKeyEvent(e, out var keyCode))
        {
            var mapped = TizenKeyboardDevice.ConvertKey(keyCode);
            if (mapped == Input.Key.None)
                return;

            Logger.TryGet(LogEventLevel.Debug, LogKey)?.Log(null, "Triggering key event {text}", e.Key.KeyString);
            SendKeyEvent(e, mapped);
        }
        else if (e.Key.State == Key.StateType.Up)
        {
            Logger.TryGet(LogEventLevel.Debug, LogKey)?.Log(null, "Triggering text input {text}", e.Key.KeyString);
            _view.TopLevelImpl.TextInput(e.Key.KeyString);
        }
    }

    private void SendKeyEvent(Window.KeyEventArgs e, Input.Key mapped)
    {
        var type = GetKeyEventType(e);
        var modifiers = GetModifierKey(e);

        _view.TopLevelImpl.Input?.Invoke(
            new RawKeyEventArgs(
                KeyboardDevice.Instance!,
                e.Key.Time,
                _view.InputRoot,
                type,
                mapped,
                modifiers));
    }

    private bool ShouldSendKeyEvent(Window.KeyEventArgs e, out global::Tizen.Uix.InputMethod.KeyCode keyCode)
    {
        if (string.IsNullOrEmpty(e.Key.KeyString) || 
            e.Key.KeyPressedName is "Delete" or "BackSpace" ||
            e.Key.IsCtrlModifier() || 
            e.Key.IsAltModifier())
        {
            if (Enum.TryParse(e.Key.KeyPressedName, true, out keyCode) ||
                Enum.TryParse($"Keypad{e.Key.KeyPressedName}", true, out keyCode))
                return true;
        }

        keyCode = 0;
        return false;
    }

    private RawKeyEventType GetKeyEventType(Window.KeyEventArgs ev)
    {
        switch (ev.Key.State)
        {
            case Key.StateType.Down:
                return RawKeyEventType.KeyDown;
            case Key.StateType.Up:
                return RawKeyEventType.KeyUp;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private RawInputModifiers GetModifierKey(Window.KeyEventArgs ev)
    {
        var modifiers = RawInputModifiers.None;

        if (ev.Key.IsShiftModifier())
            modifiers |= RawInputModifiers.Shift;

        if (ev.Key.IsAltModifier())
            modifiers |= RawInputModifiers.Alt;

        if (ev.Key.IsCtrlModifier())
            modifiers |= RawInputModifiers.Control;

        return modifiers;
    }
}
