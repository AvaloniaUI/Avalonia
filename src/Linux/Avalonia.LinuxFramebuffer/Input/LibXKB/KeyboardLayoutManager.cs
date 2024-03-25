#nullable enable
using System;
using System.Runtime.InteropServices;
using Avalonia.Input;
using static Avalonia.LinuxFramebuffer.Input.LibXKB.LibXKBNative;

namespace Avalonia.LinuxFramebuffer.Input.LibXKB
{
    internal class KeyboardLayoutManager : IDisposable
    {
        private const string LibXKB = nameof(Avalonia.LinuxFramebuffer) + "/LibXKB";
        private readonly xkb_context? _ctx;
        private xkb_keymap? _keymap;
        private xkb_state? _state;
        private bool _disposedValue;

        public KeyboardLayoutManager()
        {
            Logging.Logger.TryGet(Logging.LogEventLevel.Debug, LibXKB)
                ?.Log(this, $"XKB_DEFAULT_LAYOUT:{Environment.GetEnvironmentVariable("XKB_DEFAULT_LAYOUT")}");
            Logging.Logger.TryGet(Logging.LogEventLevel.Debug, LibXKB)
                ?.Log(this, $"XKB_DEFAULT_RULES:{Environment.GetEnvironmentVariable("XKB_DEFAULT_RULES")}");
            Logging.Logger.TryGet(Logging.LogEventLevel.Debug, LibXKB)
                ?.Log(this, $"XKB_DEFAULT_MODEL:{Environment.GetEnvironmentVariable("XKB_DEFAULT_MODEL")}");
            Logging.Logger.TryGet(Logging.LogEventLevel.Debug, LibXKB)
                ?.Log(this, $"XKB_DEFAULT_VARIANT:{Environment.GetEnvironmentVariable("XKB_DEFAULT_VARIANT")}");

            _ctx = xkb_context_new(xkb_context_flags.XKB_CONTEXT_NO_FLAGS);
            if (_ctx.IsInvalid)
            {
                Logging.Logger.TryGet(Logging.LogEventLevel.Warning, LibXKB)
                    ?.Log(this, $"{nameof(KeyboardLayoutManager)}: Failed to create xkb context Error {Marshal.GetLastWin32Error()}.");
                return;
            }

            xkb_context_set_log_level(_ctx, xkb_log_level.XKB_LOG_LEVEL_DEBUG);
            xkb_context_set_log_verbosity(_ctx, 9);

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

        public bool TryProcessKey(LibInput.libinput_key key, LibInput.libinput_key_state key_State, out (Avalonia.Input.Key Key, RawInputModifiers Modifiers, bool IsRepeats, string? Text) avaloniaKeyState)
        {
            avaloniaKeyState = default;
            if (_ctx is null or { IsInvalid: true }
                || (_keymap is null or { IsInvalid: true })
                || (_state is null or { IsInvalid: true }))
                return false;

            var keycode = (uint)key + 8;
            var sym = xkb_state_key_get_one_sym(_state, keycode);
            Logging.Logger.TryGet(Logging.LogEventLevel.Verbose, LibXKB)
                ?.Log(this, $"{key} -> {(XKBKeysEnum)sym}");

            var pressed = key_State == LibInput.libinput_key_state.Pressed;

            // Modifiers here is the modifier state before the event, i.e. not
            // including the current key in case it is a modifier. See the XOR
            // logic in QKeyEvent::modifiers(). ### QTBUG-73826
            var modifiers = AXkbCommon.GetModifiers(_state);

            var (avaloniakey, text) = AXkbCommon.KeysymToAvaloniaKey(sym, modifiers, _state, keycode);

            xkb_state_update_key(_state, keycode, pressed ? xkb_key_direction.XKB_KEY_DOWN : xkb_key_direction.XKB_KEY_UP);

            var isRepeats = pressed && xkb_keymap_key_repeats(_keymap, keycode);
            avaloniaKeyState = (avaloniakey, modifiers, isRepeats, text);
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
        ~KeyboardLayoutManager()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public bool SetKeymap(string layout)
        {
            _state?.Dispose();
            _keymap?.Dispose();

            xkb_rule_names names;

            names.rules = null;
            names.model = null;
            names.layout = layout;
            names.variant = null;
            names.options = null;

            _keymap = xkb_keymap_new_from_names(_ctx, ref names, xkb_keymap_compile_flags.XKB_KEYMAP_COMPILE_NO_FLAGS);
            if (_keymap.IsInvalid)
            {
                Logging.Logger.TryGet(Logging.LogEventLevel.Warning, LibXKB)
                    ?.Log(this, $"{nameof(KeyboardLayoutManager)}: Failed to compile keymap Error {Marshal.GetLastWin32Error()}.");
                return false;
            }


            _state = xkb_state_new(_keymap);
            if (_state.IsInvalid)
            {
                Logging.Logger.TryGet(Logging.LogEventLevel.Warning, LibXKB)
                    ?.Log(this, $"{nameof(KeyboardLayoutManager)}: Failed to compile keymap Error {Marshal.GetLastWin32Error()}.");
                return false;
            }

            return true;
        }
    }
}
