#nullable enable
using System;
using Avalonia.Input;
using static Avalonia.LinuxFramebuffer.Input.LibXKB.LibXKBNative;

namespace Avalonia.LinuxFramebuffer.Input.LibXKB
{
    internal class KeyboardManager : IDisposable
    {
        private const string LibXKB = nameof(Logging.LogArea.X11Platform) + "/LibXKB";
        private readonly xkb_context? _ctx;
        private readonly xkb_keymap? _keymap;
        private readonly xkb_state? _state;
        private bool _disposedValue;

        public unsafe KeyboardManager()
        {
            _ctx = xkb_context_new(xkb_context_flags.XKB_CONTEXT_NO_FLAGS);
            if (!_ctx.IsInvalid)
            {
                Logging.Logger.TryGet(Logging.LogEventLevel.Warning, LibXKB)
                    ?.Log(this, $"{nameof(KeyboardManager)}: Failed to create xkb context.");
                return;
            }

            _keymap = xkb_keymap_new_from_names(_ctx, default, xkb_keymap_compile_flags.XKB_KEYMAP_COMPILE_NO_FLAGS);
            if (!_keymap.IsInvalid)
            {
                Logging.Logger.TryGet(Logging.LogEventLevel.Warning, LibXKB)
                    ?.Log(this, $"{nameof(KeyboardManager)}: Failed to compile keymap.");
                return;
            }
            _state = xkb_state_new(_keymap);
            if (!_state.IsInvalid)
            {
                Logging.Logger.TryGet(Logging.LogEventLevel.Warning, LibXKB)
                    ?.Log(this, $"{nameof(KeyboardManager)}: Failed to create xkb state.");
                return;
            }
        }

        public bool TtyGetModifiers(out RawInputModifiers modifiers)
        {
            modifiers = default;
            if (_ctx is null or { IsInvalid: true }
                || (_keymap is null or { IsInvalid: true })
                || (_state is null or { IsInvalid: true }))
                return false;
            modifiers = AXkbCommon.GetModifiers(_state);
            return true;
        }

        public bool TryProcessKey(LibInput.libinput_key key, LibInput.libinput_key_state key_State, out (Avalonia.Input.Key Key, RawInputModifiers Modifiers, bool IsRepeats)? avaloniaKeyState)
        {
            avaloniaKeyState = default;
            if (_ctx is null or { IsInvalid: true }
                || (_keymap is null or { IsInvalid: true })
                || (_state is null or { IsInvalid: true }))
                return false;

            uint keycode = (uint)key + 8;
            var sym = xkb_state_key_get_one_sym(_state, keycode);
            var pressed = key_State == LibInput.libinput_key_state.Pressed;

            // Modifiers here is the modifier state before the event, i.e. not
            // including the current key in case it is a modifier. See the XOR
            // logic in QKeyEvent::modifiers(). ### QTBUG-73826
            RawInputModifiers modifiers = AXkbCommon.GetModifiers(_state);

            //var text = AXkbCommon.GetLookupString(_state, keycode);
            Avalonia.Input.Key avaloniakey = AXkbCommon.KeysymToAvaloniaKey(sym, modifiers, _state, keycode);

            xkb_state_update_key(_state, keycode, pressed ? xkb_key_direction.XKB_KEY_DOWN : xkb_key_direction.XKB_KEY_UP);

            // RawInputModifiers modifiersAfterStateChange = AXkbCommon.GetModifiers(m_state);
            //QGuiApplicationPrivate::inputDeviceManager()->setKeyboardModifiers(modifiersAfterStateChange);

            //QWindowSystemInterface::handleExtendedKeyEvent(nullptr,
            //                                               pressed ? QEvent::KeyPress : QEvent::KeyRelease,
            //                                               qtkey, modifiers, keycode, sym, modifiers, text);

            var isRepeats = pressed && xkb_keymap_key_repeats(_keymap, keycode);
            avaloniaKeyState = (avaloniakey, modifiers, isRepeats);
            return true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _state?.Dispose();
                _keymap?.Dispose();
                _ctx?.Dispose();
                _disposedValue = true;
            }
        }
        ~KeyboardManager()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
