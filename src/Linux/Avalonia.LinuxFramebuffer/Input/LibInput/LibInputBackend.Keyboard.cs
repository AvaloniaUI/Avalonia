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
            AvaloniaLocator.Current.GetRequiredService<IKeyboardDevice>();
        private readonly KeyboardLayoutManager _layoutManager = new();

        private void HandleKeyboardEvent(IntPtr rawEvent, LibInputEventType type)
        {
            var rawKeyboardEvent = libinput_event_get_keyboard_event(rawEvent);

            if (rawKeyboardEvent != IntPtr.Zero)
            {
                var key = libinput_event_keyboard_get_key(rawKeyboardEvent);
                var state = libinput_event_keyboard_get_key_state(rawKeyboardEvent);
                var timestamp = libinput_event_keyboard_get_time(rawKeyboardEvent);

                switch (state)
                {
                    case libinput_key_state.Pressed:
                        OnKeyPressEvent(key, timestamp);
                        break;
                    case libinput_key_state.Released:
                        OnKeyReleaseEvent(key, timestamp);
                        break;
                    default:
                    {
                            Logging.Logger.TryGet(Logging.LogEventLevel.Warning, nameof(LinuxFramebuffer)  + "/LibXKB")
                                ?.Log(this, $"{nameof(HandleKeyboardEvent)}: Unsupported state {state} for {key}");
                        break;
                    }
                }
            }
            else
            {
                Logging.Logger.TryGet(Logging.LogEventLevel.Verbose, LibInput)
                    ?.Log(this, $"HandleKeyboardEvent Invalid IntPtr");
            }
        }

        private void OnKeyPressEvent(libinput_key rawKey, ulong timestamp)
        {
            if (_layoutManager.TryProcessKey(rawKey, libinput_key_state.Pressed, out var state))
            {
                Logging.Logger.TryGet(Logging.LogEventLevel.Debug, nameof(LinuxFramebuffer)  + "/LibXKB")
                    ?.Log(this, $"{nameof(OnKeyPressEvent)}: {rawKey} -> {state.Key} Modifiers:{state.Modifiers}");

                var args = new RawKeyEventArgsWithText(_keyboard
                    , timestamp
                    , _inputRoot
                    , RawKeyEventType.KeyDown
                    , state.Key
                    , state.Modifiers
                    , state.Text);
                ScheduleInput(args);
            }
            else
            {
                Logging.Logger.TryGet(Logging.LogEventLevel.Error, nameof(LinuxFramebuffer)  + "/LibXKB")
                    ?.Log(this, $"{nameof(OnKeyPressEvent)}: Invalid key mapping of {rawKey}.");
            }
        }

        private void OnKeyReleaseEvent(libinput_key rawKey, ulong timestamp)
        {
            if (_layoutManager.TryProcessKey(rawKey, libinput_key_state.Released, out var state))
            {
                Logging.Logger.TryGet(Logging.LogEventLevel.Debug, LibInput)
                    ?.Log(this, $"{nameof(OnKeyReleaseEvent)}: {rawKey} -> {state.Key} Modifiers:{state.Modifiers}");

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
                Logging.Logger.TryGet(Logging.LogEventLevel.Error, nameof(LinuxFramebuffer)  + "/LibXKB")
                    ?.Log(this, $"{nameof(OnKeyReleaseEvent)}: Invalid key mapping of {rawKey}.");
            }
        }
        
        /// <summary>
        /// RawKeyEventArgsWithText is used to attach the text value of the key to an asynchronously dispatched KeyDown event
        /// </summary>
        private class RawKeyEventArgsWithText : RawKeyEventArgs
        {
            public RawKeyEventArgsWithText(IKeyboardDevice device, ulong timestamp, IInputRoot root,
                RawKeyEventType type, Key key, RawInputModifiers modifiers, string? text) :
                base(device, timestamp, root, type, key, modifiers)
            {
                Text = text;
            }
            
            public string? Text { get; }
        }

    }
}
