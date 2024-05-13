using System;
using System.Diagnostics;
using System.Text;
using Avalonia.FreeDesktop;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform.Interop;
using Avalonia.Threading;
using NWayland.Protocols.Wayland;

namespace Avalonia.Wayland
{
    internal class WlKeyboardHandler : WlKeyboard.IEvents, IDisposable
    {
        private readonly AvaloniaWaylandPlatform _platform;
        private readonly WlInputDevice _wlInputDevice;
        private readonly IKeyboardDevice _keyboardDevice;
        private readonly IInputContext _inputContext;
        private readonly WlKeyboard _wlKeyboard;
        private readonly IntPtr _xkbContext;

        private readonly Utf8Buffer _controlBuffer;
        private readonly Utf8Buffer _mod1Buffer;
        private readonly Utf8Buffer _shiftBuffer;
        private readonly Utf8Buffer _mod4Buffer;

        private WlWindow? _window;

        private IntPtr _xkbKeymap;
        private IntPtr _xkbState;

        private TimeSpan _repeatDelay;
        private TimeSpan _repeatInterval;
        private bool _firstRepeat;
        private KeyboardInputState _repeatState;
        private IDisposable? _keyboardTimer;

        public WlKeyboardHandler(AvaloniaWaylandPlatform platform, WlInputDevice wlInputDevice)
        {
            _platform = platform;
            _wlInputDevice = wlInputDevice;
            _wlKeyboard = platform.WlSeat.GetKeyboard();
            _wlKeyboard.Events = this;
            _xkbContext = LibXkbCommon.xkb_context_new(0);
            _keyboardDevice = AvaloniaLocator.Current.GetRequiredService<IKeyboardDevice>();
            if (EnableIme(platform.Options))
                _inputContext = new TextInputInputContext();
            _inputContext = new ComposeInputContext(_xkbContext);
            _controlBuffer = new Utf8Buffer("Control");
            _mod1Buffer = new Utf8Buffer("Mod1");
            _shiftBuffer = new Utf8Buffer("Shift");
            _mod4Buffer = new Utf8Buffer("Mod4");
        }

        public uint KeyboardEnterSerial { get; private set; }

        public void OnKeymap(WlKeyboard eventSender, WlKeyboard.KeymapFormatEnum format, int fd, uint size)
        {
            if (format != WlKeyboard.KeymapFormatEnum.XkbV1)
                return;

            var mapStr = LibC.mmap(IntPtr.Zero, (int)size, MemoryProtection.PROT_READ, SharingType.MAP_PRIVATE, fd, 0);
            if (mapStr == new IntPtr(-1))
            {
                LibC.close(fd);
                return;
            }

            var xkbKeymap = LibXkbCommon.xkb_keymap_new_from_string(_xkbContext, mapStr, (uint)format, 0);
            LibC.munmap(mapStr, (int)size);
            LibC.close(fd);

            LibXkbCommon.xkb_keymap_unref(_xkbKeymap);
            _xkbKeymap = xkbKeymap;

            if (xkbKeymap == IntPtr.Zero)
                return;

            var xkbState = LibXkbCommon.xkb_state_new(_xkbKeymap);
            LibXkbCommon.xkb_state_unref(_xkbState);
            _xkbState = xkbState;
        }

        public void OnEnter(WlKeyboard eventSender, uint serial, WlSurface surface, ReadOnlySpan<int> keys)
        {
            _window = _platform.WlScreens.WindowFromSurface(surface);
            _wlInputDevice.Serial = serial;
            KeyboardEnterSerial = serial;
            _window?.Activated?.Invoke();
        }

        public void OnLeave(WlKeyboard eventSender, uint serial, WlSurface surface)
        {
            _wlInputDevice.Serial = serial;
            var window = _platform.WlScreens.WindowFromSurface(surface);
            window?.LostFocus?.Invoke();
            if (window == _window)
                _window = null;
        }

        public void OnKey(WlKeyboard eventSender, uint serial, uint time, uint key, WlKeyboard.KeyStateEnum state)
        {
            _wlInputDevice.Serial = serial;

            if (_window?.InputRoot is null)
                return;

            var inputState = new KeyboardInputState();
            inputState.Time = time;
            inputState.KeyCode = key + 8;
            inputState.Sym = LibXkbCommon.xkb_state_key_get_one_sym(_xkbState, inputState.KeyCode);
            inputState.Key = GetAvaloniaKey(inputState.Sym, inputState.KeyCode);
            inputState.PhysicalKey = XkbKeyTransform.PhysicalKeyFromScanCode(inputState.KeyCode);
            inputState.EventType = state == WlKeyboard.KeyStateEnum.Pressed ? RawKeyEventType.KeyDown : RawKeyEventType.KeyUp;
            inputState.Text = LookupString(inputState.KeyCode);

            HandleKey(ref inputState);

            if (state == WlKeyboard.KeyStateEnum.Pressed)
            {
                _wlInputDevice.UserActionDownSerial = serial;
                if (LibXkbCommon.xkb_keymap_key_repeats(_xkbKeymap, inputState.KeyCode) && _repeatInterval > TimeSpan.Zero)
                {
                    _keyboardTimer?.Dispose();
                    _repeatState = inputState;
                    _firstRepeat = true;
                    _keyboardTimer = DispatcherTimer.Run(OnRepeatKey, _repeatDelay, DispatcherPriority.Input);
                }
            }
            else if (_repeatState.KeyCode == inputState.KeyCode)
            {
                _keyboardTimer?.Dispose();
                _keyboardTimer = null;
            }
        }

        public void OnModifiers(WlKeyboard eventSender, uint serial, uint modsDepressed, uint modsLatched, uint modsLocked, uint group)
        {
            _wlInputDevice.Serial = serial;
            LibXkbCommon.xkb_state_update_mask(_xkbState, modsDepressed, modsLatched, modsLocked, 0, 0, group);

            if (LibXkbCommon.xkb_state_mod_name_is_active(_xkbState, _controlBuffer, LibXkbCommon.XkbStateComponent.XKB_STATE_MODS_EFFECTIVE) > 0)
                _wlInputDevice.RawInputModifiers |= RawInputModifiers.Control;
            else
                _wlInputDevice.RawInputModifiers &= ~RawInputModifiers.Control;
            if (LibXkbCommon.xkb_state_mod_name_is_active(_xkbState, _mod1Buffer, LibXkbCommon.XkbStateComponent.XKB_STATE_MODS_EFFECTIVE) > 0)
                _wlInputDevice.RawInputModifiers |= RawInputModifiers.Alt;
            else
                _wlInputDevice.RawInputModifiers &= ~RawInputModifiers.Alt;
            if (LibXkbCommon.xkb_state_mod_name_is_active(_xkbState, _shiftBuffer, LibXkbCommon.XkbStateComponent.XKB_STATE_MODS_EFFECTIVE) > 0)
                _wlInputDevice.RawInputModifiers |= RawInputModifiers.Shift;
            else
                _wlInputDevice.RawInputModifiers &= ~RawInputModifiers.Shift;
            if (LibXkbCommon.xkb_state_mod_name_is_active(_xkbState, _mod4Buffer, LibXkbCommon.XkbStateComponent.XKB_STATE_MODS_EFFECTIVE) > 0)
                _wlInputDevice.RawInputModifiers |= RawInputModifiers.Meta;
            else
                _wlInputDevice.RawInputModifiers &= ~RawInputModifiers.Meta;
        }

        public void OnRepeatInfo(WlKeyboard eventSender, int rate, int delay)
        {
            _repeatDelay = TimeSpan.FromMilliseconds(delay);
            _repeatInterval = TimeSpan.FromSeconds(1d / rate);
        }

        public void Dispose()
        {
            if (_xkbContext != IntPtr.Zero)
                LibXkbCommon.xkb_context_unref(_xkbContext);
            LibXkbCommon.xkb_keymap_unref(_xkbKeymap);
            LibXkbCommon.xkb_state_unref(_xkbState);
            _wlKeyboard.Dispose();
            _keyboardTimer?.Dispose();
        }

        internal void InvalidateFocus(WlWindow window)
        {
            if (_window == window)
                _window = null;
        }

        private void HandleKey(ref KeyboardInputState state)
        {
            if (_window?.InputRoot is null)
                return;

            var filtered = _inputContext.HandleEvent(_window, ref state);

            if (!filtered)
            {
                RawInputEventArgs args = new RawKeyEventArgs(_keyboardDevice, state.Time, _window.InputRoot, state.EventType, state.Key, _wlInputDevice.RawInputModifiers, state.PhysicalKey, state.Text);
                _window.Input?.Invoke(args);
                if (state.EventType == RawKeyEventType.KeyDown && !args.Handled && state.Text?.Length > 0)
                {
                    args = new RawTextInputEventArgs(_keyboardDevice, state.Time, _window.InputRoot, state.Text);
                    _window.Input?.Invoke(args);
                }
            }
        }

        private bool OnRepeatKey()
        {
            HandleKey(ref _repeatState);
            if (!_firstRepeat)
                return true;
            _firstRepeat = false;
            _keyboardTimer?.Dispose();
            _keyboardTimer = DispatcherTimer.Run(OnRepeatKey, _repeatInterval, DispatcherPriority.Input);
            return false;
        }

        private Key GetAvaloniaKey(XkbKey sym, uint code)
        {
            if (_wlInputDevice.RawInputModifiers.HasAllFlags(RawInputModifiers.Control))
            {
                var latinKeySym = GetLatinKey(code);
                if (latinKeySym != 0)
                    sym = (XkbKey)latinKeySym;
            }

            return XkbKeyTransform.ConvertKey(sym);
        }

        private unsafe string? LookupString(uint code)
        {
            const int Length = 32;
            var chars = stackalloc byte[Length];
            var size = LibXkbCommon.xkb_state_key_get_utf8(_xkbState, code, chars, Length);
            if (size + 1 > Length)
            {
                var chars1 = stackalloc byte[size + 1];
                chars = chars1;
                LibXkbCommon.xkb_state_key_get_utf8(_xkbState, code, chars, Length);
            }

            var text = Encoding.UTF8.GetString(chars, size);

            if (text.Length == 1 && (text[0] < ' ' || text[0] == 0x7f)) // Control codes or DEL
                return null;
            return text;
        }

        private unsafe uint GetLatinKey(uint code)
        {
            var sym = 0u;
            var keymap = LibXkbCommon.xkb_state_get_keymap(_xkbState);
            var layoutCount = LibXkbCommon.xkb_keymap_num_layouts_for_key(keymap, code);
            for (var layout = 0u; layout < layoutCount; layout++)
            {
                uint* syms;
                var level = LibXkbCommon.xkb_state_key_get_level(_xkbState, code, layout);
                if (LibXkbCommon.xkb_keymap_key_get_syms_by_level(keymap, code, layout, level, &syms) != 1)
                    continue;
                if (!IsLatin1(syms[0]))
                    continue;
                sym = syms[0];
                break;
            }

            return sym;
        }

        private static bool IsLatin1(uint c) => c < 256;

        private static bool EnableIme(WaylandPlatformOptions options)
        {
            // Disable if explicitly asked by user
            var avaloniaImModule = Environment.GetEnvironmentVariable("AVALONIA_IM_MODULE");
            if (avaloniaImModule == "none")
                return false;

            // Use value from options when specified
            if (options.EnableIme.HasValue)
                return options.EnableIme.Value;

            // Automatically enable for CJK locales
            var lang = Environment.GetEnvironmentVariable("LANG");
            var isCjkLocale = lang is not null &&
                              (lang.Contains("zh")
                               || lang.Contains("ja")
                               || lang.Contains("vi")
                               || lang.Contains("ko"));

            return isCjkLocale;
        }
    }
}
