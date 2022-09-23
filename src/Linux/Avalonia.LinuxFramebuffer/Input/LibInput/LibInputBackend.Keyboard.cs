using System;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.LinuxFramebuffer.Input.LibXKB;
using static Avalonia.LinuxFramebuffer.Input.LibInput.LibInputNativeUnsafeMethods;
#nullable enable

namespace Avalonia.LinuxFramebuffer.Input.LibInput
{
    public partial class LibInputBackend
    {
        private readonly IKeyboardDevice _keyboard =
            AvaloniaLocator.Current.GetRequiredService<IKeyboardDevice>() ??
             new KeyboardDevice();
        private readonly KeyboardManager _manager =
            new KeyboardManager();

        private void HandleKeyboardEvent(IntPtr rawEvent, LibInputEventType type)
        {
            var rawKeyboardEvent = libinput_event_get_keyboard_event(rawEvent);

            if (rawKeyboardEvent != IntPtr.Zero)
            {
                var key = libinput_event_keyboard_get_key(rawKeyboardEvent);
                var state = libinput_event_keyboard_get_key_state(rawKeyboardEvent);
                var timestamp = (ulong)libinput_event_keyboard_get_time(rawKeyboardEvent);

                if (state == libinput_key_state.Pressed)
                {
                    OnKeyPressEvent(key, timestamp);
                }
                else if (state == libinput_key_state.Released)
                {
                    OnKeyReleaseEvent(key, timestamp);
                }
                else
                {
                    if (Logging.Logger.IsEnabled(Logging.LogEventLevel.Warning, LibInput))
                    {
                        Logging.Logger.TryGet(Logging.LogEventLevel.Warning, LibInput)
                            ?.Log(this, $"{nameof(HandleKeyboardEvent)}: Unsupported state {state} for {key}");
                    }
                }
            }
            else
            {
                Logging.Logger.TryGet(Logging.LogEventLevel.Verbose, LibInput)
                    ?.Log(this, $"HandleKeyboardEvent Invalid IntPtre");
            }
        }

        private void OnKeyPressEvent(libinput_key rawKey, ulong timestamp)
        {
            if (_manager.TryProcessKey(rawKey, libinput_key_state.Pressed, out var state))
            {
                Logging.Logger.TryGet(Logging.LogEventLevel.Debug, LibInput)
                    ?.Log(this, $"{nameof(OnKeyPressEvent)}: {rawKey} -> {state.Key}");

                var args = new RawKeyEventArgs(_keyboard
                    , timestamp
                    , _inputRoot
                    , RawKeyEventType.KeyDown
                    , state.Key
                    , state.Modifiers);
                ScheduleInput(args);
            }
            else
            {
                Logging.Logger.TryGet(Logging.LogEventLevel.Error, LibInput)
                    ?.Log(this, $"{nameof(OnKeyPressEvent)}: Invalid key mapping of {rawKey}.");
            }
        }

        private void OnKeyReleaseEvent(libinput_key rawKey, ulong timestamp)
        {
            if (_manager.TryProcessKey(rawKey, libinput_key_state.Pressed, out var state))
            {
                Logging.Logger.TryGet(Logging.LogEventLevel.Debug, LibInput)
                    ?.Log(this, $"{nameof(OnKeyReleaseEvent)}: {rawKey} -> {state.Key}");

                var args = new RawKeyEventArgs(_keyboard
                    , timestamp
                    , _inputRoot
                    , RawKeyEventType.KeyUp
                    , state.Key
                    , state.Modifiers);
                ScheduleInput(args);
            }
            else
            {
                Logging.Logger.TryGet(Logging.LogEventLevel.Error, LibInput)
                    ?.Log(this, $"{nameof(OnKeyReleaseEvent)}: Invalid key mapping of {rawKey}.");
            }
        }


    }
}
