using System;
using System.Text;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform.Interop;

namespace Avalonia.Wayland
{
    internal class ComposeInputContext : IInputContext, IDisposable
    {
        private readonly IKeyboardDevice _keyboardDevice;
        private readonly IntPtr _xkbContext;

        private bool _isInitialized;
        private IntPtr _xkbComposeTable;
        private IntPtr _xkbComposeState;

        public ComposeInputContext(IntPtr xkbContext)
        {
            _xkbContext = xkbContext;
            _keyboardDevice = AvaloniaLocator.Current.GetRequiredService<IKeyboardDevice>();
        }

        public unsafe bool HandleEvent(WlWindow window, ref KeyboardInputState state)
        {
            if (state.EventType != RawKeyEventType.KeyDown)
                return false;

            EnsureInitialized();

            LibXkbCommon.xkb_compose_state_feed(_xkbComposeState, state.Sym);
            var status = LibXkbCommon.xkb_compose_state_get_status(_xkbComposeState);

            switch (status)
            {
                case LibXkbCommon.XkbComposeStatus.XKB_COMPOSE_COMPOSING:
                    return true;
                case LibXkbCommon.XkbComposeStatus.XKB_COMPOSE_COMPOSED:
                    var size = LibXkbCommon.xkb_compose_state_get_utf8(_xkbComposeState, null, 0);
                    var buffer = stackalloc byte[size + 1];
                    LibXkbCommon.xkb_compose_state_get_utf8(_xkbComposeState, buffer, size + 1);
                    var text = Encoding.UTF8.GetString(buffer, size);
                    LibXkbCommon.xkb_compose_state_reset(_xkbComposeState);
                    window.Input?.Invoke(new RawTextInputEventArgs(_keyboardDevice, state.Time, window.InputRoot!, text));
                    return true;
                case LibXkbCommon.XkbComposeStatus.XKB_COMPOSE_CANCELLED:
                    LibXkbCommon.xkb_compose_state_reset(_xkbComposeState);
                    return false;
                case LibXkbCommon.XkbComposeStatus.XKB_COMPOSE_NOTHING:
                default:
                    return false;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            LibXkbCommon.xkb_compose_state_unref(_xkbComposeState);
            LibXkbCommon.xkb_compose_table_unref(_xkbComposeTable);
        }

        private void EnsureInitialized()
        {
            if (_isInitialized)
                return;

            _isInitialized = true;
            var locale = Environment.GetEnvironmentVariable("LC_ALL")
                            ?? Environment.GetEnvironmentVariable("LC_CTYPE")
                            ?? Environment.GetEnvironmentVariable("LANG")
                            ?? "C";
            using var utf8buffer = new Utf8Buffer(locale);
            _xkbComposeTable = LibXkbCommon.xkb_compose_table_new_from_locale(_xkbContext, utf8buffer, 0);
            if (_xkbComposeTable != IntPtr.Zero)
                _xkbComposeState = LibXkbCommon.xkb_compose_state_new(_xkbComposeTable, 0);
        }
    }
}
