using System.Diagnostics;
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
            Logger.TryGet(LogEventLevel.Debug, LogKey)?.Log(null, "Triggering key event {text}", e.Key.KeyString);
            SendKeyEvent(e, keyCode);
        }
        else if (e.Key.State == Key.StateType.Up && !string.IsNullOrEmpty(e.Key.KeyString))
        {
            Logger.TryGet(LogEventLevel.Debug, LogKey)?.Log(null, "Triggering text input {text}", e.Key.KeyString);
            _view.TopLevelImpl.TextInput(e.Key.KeyString);
        }
    }

    private void SendKeyEvent(Window.KeyEventArgs e, Input.Key mapped)
    {
        var type = GetKeyEventType(e);
        var modifiers = GetModifierKey(e);
        var deviceType = GetDeviceType(e);

        _view.TopLevelImpl.Input?.Invoke(
            new RawKeyEventArgs(
                KeyboardDevice.Instance!,
                e.Key.Time,
                _view.InputRoot,
                type,
                mapped,
                modifiers,
                PhysicalKey.None,
                deviceType,
                e.Key.KeyString
                ));
    }

    private bool ShouldSendKeyEvent(Window.KeyEventArgs e, out Input.Key keyCode)
    {
        keyCode = TizenKeyboardDevice.GetSpecialKey(e.Key.KeyPressedName);
        if (keyCode != Input.Key.None)
            return true;

        if ((e.Key.IsCtrlModifier() || e.Key.IsAltModifier()) && !string.IsNullOrEmpty(e.Key.KeyString))
        {
            var c = e.Key.KeyPressedName.Length == 1 ? e.Key.KeyPressedName[0] : (char)e.Key.KeyCode;
            return (keyCode = TizenKeyboardDevice.GetAsciiKey(c)) != Input.Key.None;
        }

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

    private KeyDeviceType GetDeviceType(Window.KeyEventArgs ev)
    {
        if (ev.Key.DeviceClass == DeviceClassType.Gamepad)
            return KeyDeviceType.Gamepad;

        if (ev.Key.DeviceSubClass == DeviceSubClassType.Remocon)
            return KeyDeviceType.Remote;

        return KeyDeviceType.Keyboard;
    }
}
