using System;
using System.Collections.Generic;
using Avalonia.Collections.Pooled;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Foundation;
using UIKit;

namespace Avalonia.iOS;

internal sealed class InputHandler
{
    private static readonly PooledList<RawPointerPoint> s_intermediatePointsPooledList = new(ClearMode.Never);

    private readonly AvaloniaView _view;
    private readonly ITopLevelImpl _tl;
    private readonly TouchDevice _touchDevice = new();
    private readonly MouseDevice _mouseDevice = new();
    private readonly PenDevice _penDevice = new();
    private static long _nextTouchPointId = 1;
    private readonly Dictionary<UITouch, long> _knownTouches = new();

    public InputHandler(AvaloniaView view, ITopLevelImpl tl)
    {
        _view = view;
        _tl = tl;
    }

    private static ulong Ts(UIEvent? evt) => evt is null ? 0 : (ulong)(evt.Timestamp * 1000);
    private IInputRoot Root => _view.InputRoot;

    public void Handle(NSSet touches, UIEvent? evt)
    {
        foreach (UITouch t in touches)
        {
            if (t.Type == UITouchType.Indirect)
            {
                // Ignore Indirect input, like remote controller trackpad.
                // For Avalonia we handle it independently with gestures.
                continue;
            }

            if (!_knownTouches.TryGetValue(t, out var id))
                _knownTouches[t] = id = _nextTouchPointId++;

            IInputDevice device = t.Type switch
            {
                UITouchType.Stylus => _penDevice,
#pragma warning disable CA1416
                UITouchType.IndirectPointer => _mouseDevice,
#pragma warning restore CA1416
                _ => _touchDevice
            };

            var modifiers = RawInputModifiers.None;
            if (OperatingSystem.IsIOSVersionAtLeast(13, 4)
                 || OperatingSystem.IsTvOSVersionAtLeast(13, 4))
            {
                modifiers = ConvertModifierKeys(evt?.ModifierFlags);
            }
            
            var ev = new RawTouchEventArgs(device, Ts(evt), Root,
                (device, t.Phase) switch
                {
                    (TouchDevice, UITouchPhase.Began) => RawPointerEventType.TouchBegin,
                    (TouchDevice, UITouchPhase.Ended) => RawPointerEventType.TouchEnd,
                    (TouchDevice, UITouchPhase.Cancelled) => RawPointerEventType.TouchCancel,
                    (TouchDevice, _) => RawPointerEventType.TouchUpdate,
                    
                    (_, UITouchPhase.Began) => IsRightClick() ? RawPointerEventType.RightButtonDown : RawPointerEventType.LeftButtonDown,
                    (_, UITouchPhase.Ended or UITouchPhase.Cancelled) => IsRightClick() ? RawPointerEventType.RightButtonUp : RawPointerEventType.RightButtonDown,
                    (_, _) => RawPointerEventType.Move,
                }, ToPointerPoint(t), modifiers, id)
            {
                IntermediatePoints = evt is {} thisEvent ? new Lazy<IReadOnlyList<RawPointerPoint>?>(() =>
                {
                    var coalesced = thisEvent.GetCoalescedTouches(t) ?? Array.Empty<UITouch>();
                    s_intermediatePointsPooledList.Clear();
                    s_intermediatePointsPooledList.Capacity = coalesced.Length - 1;

                    // Skip the last one, as it is already processed point.
                    for (var i = 0; i < coalesced.Length - 1; i++)
                    {
                        s_intermediatePointsPooledList.Add(ToPointerPoint(coalesced[i]));
                    }

                    return s_intermediatePointsPooledList;
                }) : null
            };

            _tl.Input?.Invoke(ev);

            if (t.Phase is UITouchPhase.Cancelled or UITouchPhase.Ended)
                _knownTouches.Remove(t);

            RawPointerPoint ToPointerPoint(UITouch touch) => new()
            {
                Position = touch.LocationInView(_view).ToAvalonia(),
                // in iOS "1.0 represents the force of an average touch", when Avalonia expects 0.5 for "average".
                // If MaximumPossibleForce is 0, we ignore it completely.
                Pressure = t.MaximumPossibleForce == 0 ? 0.5f : (float)t.Force / 2
            };

            bool IsRightClick()
#if !TVOS
                => OperatingSystem.IsIOSVersionAtLeast(13, 4) && (evt?.ButtonMask.HasFlag(UIEventButtonMask.Secondary) ?? false);
#else
                => false;
#endif
        }
    }

    public bool Handle(NSSet<UIPress> presses, UIPressesEvent? evt)
    {
        var handled = false;
        foreach (UIPress p in presses)
        {
            PhysicalKey physicalKey;
            RawInputModifiers modifier = default;
            string? characters = null;
            KeyDeviceType keyDeviceType;

            if ((OperatingSystem.IsIOSVersionAtLeast(13, 4)
                || OperatingSystem.IsTvOSVersionAtLeast(13, 4))
                && p.Key is { } uiKey
                && s_keys.TryGetValue(uiKey.KeyCode, out physicalKey))
            {
                modifier = ConvertModifierKeys(uiKey.ModifierFlags);

                keyDeviceType = KeyDeviceType.Keyboard; // very likely

                if (!uiKey.Characters.StartsWith("UIKey"))
                    characters = uiKey.Characters;
            }
            else
            {
                physicalKey = p.Type switch
                {
                    UIPressType.UpArrow => PhysicalKey.ArrowUp,
                    UIPressType.DownArrow => PhysicalKey.ArrowDown,
                    UIPressType.LeftArrow => PhysicalKey.ArrowLeft,
                    UIPressType.RightArrow => PhysicalKey.ArrowRight,
                    UIPressType.Select => PhysicalKey.Space,
                    UIPressType.Menu => PhysicalKey.ContextMenu,
                    UIPressType.PlayPause => PhysicalKey.MediaPlayPause,
#pragma warning disable CA1416
                    UIPressType.PageUp => PhysicalKey.PageUp,
                    UIPressType.PageDown => PhysicalKey.PageDown,
#pragma warning restore CA1416
                    _ => PhysicalKey.None
                };
                keyDeviceType = KeyDeviceType.Remote; // very likely
            }

            var key = physicalKey.ToQwertyKey();
            if (key == Key.None)
                continue;

            var ev = new RawKeyEventArgs(KeyboardDevice.Instance!, Ts(evt), Root,
                p.Phase switch
                {
                    UIPressPhase.Began => RawKeyEventType.KeyDown,
                    UIPressPhase.Changed => RawKeyEventType.KeyDown,
                    UIPressPhase.Stationary => RawKeyEventType.KeyDown,
                    UIPressPhase.Ended => RawKeyEventType.KeyUp,
                    _ => RawKeyEventType.KeyUp
                }, key, modifier, physicalKey, keyDeviceType, characters);

            _tl.Input?.Invoke(ev);
            handled |= ev.Handled;

            if (!ev.Handled && p.Phase == UIPressPhase.Began && !string.IsNullOrEmpty(characters))
            {
                var rawTextEvent = new RawTextInputEventArgs(
                    KeyboardDevice.Instance!,
                    Ts(evt),
                    _view.InputRoot,
                    characters
                );
                _tl.Input?.Invoke(rawTextEvent);
                handled |= rawTextEvent.Handled;
            }
        }

        return handled;
    }

    public void Handle(UISwipeGestureRecognizer recognizer)
    {
        var handled = false;
        var direction = recognizer.Direction;
        var timestamp = 0UL; // todo

        if (OperatingSystem.IsTvOS())
        {
            if (direction.HasFlag(UISwipeGestureRecognizerDirection.Up))
                handled = handled || HandleNavigationKey(Key.Up);
            if (direction.HasFlag(UISwipeGestureRecognizerDirection.Right))
                handled = handled || HandleNavigationKey(Key.Right);
            if (direction.HasFlag(UISwipeGestureRecognizerDirection.Down))
                handled = handled || HandleNavigationKey(Key.Down);
            if (direction.HasFlag(UISwipeGestureRecognizerDirection.Left))
                handled = handled || HandleNavigationKey(Key.Left);
        }

        if (!handled)
        {
            // TODO raise RawPointerGestureEventArgs
        }

        bool HandleNavigationKey(Key key)
        {
            // Don't pass PhysicalKey, as physically it's just a touch gesture.
            var ev = new RawKeyEventArgs(KeyboardDevice.Instance!, timestamp, Root,
                RawKeyEventType.KeyDown, key, RawInputModifiers.None, PhysicalKey.None, KeyDeviceType.Remote, null);
            _tl.Input?.Invoke(ev);
            var handled = ev.Handled;

            ev.Handled = false;
            ev.Type = RawKeyEventType.KeyUp;
            _tl.Input?.Invoke(ev);
            handled |= ev.Handled;

            return handled;
        }
    }

    private static RawInputModifiers ConvertModifierKeys(UIKeyModifierFlags? uiModifier)
    {
        RawInputModifiers modifier = default;
        if (uiModifier is { } flags)
        {
            if (flags.HasFlag(UIKeyModifierFlags.Shift))
                modifier |= RawInputModifiers.Shift;
            if (flags.HasFlag(UIKeyModifierFlags.Alternate))
                modifier |= RawInputModifiers.Alt;
            if (flags.HasFlag(UIKeyModifierFlags.Control))
                modifier |= RawInputModifiers.Control;
            if (flags.HasFlag(UIKeyModifierFlags.Command))
                modifier |= RawInputModifiers.Meta;
        }

        return modifier;
    }

#pragma warning disable CA1416
    private static Dictionary<UIKeyboardHidUsage, PhysicalKey> s_keys = new()
    {
        //[UIKeyboardHidUsage.KeyboardErrorRollOver] = PhysicalKey.None,
        //[UIKeyboardHidUsage.KeyboardPostFail] = PhysicalKey.None,
        //[UIKeyboardHidUsage.KeyboardErrorUndefined] = PhysicalKey.None,
        [UIKeyboardHidUsage.KeyboardA] = PhysicalKey.A,
        [UIKeyboardHidUsage.KeyboardB] = PhysicalKey.B,
        [UIKeyboardHidUsage.KeyboardC] = PhysicalKey.C,
        [UIKeyboardHidUsage.KeyboardD] = PhysicalKey.D,
        [UIKeyboardHidUsage.KeyboardE] = PhysicalKey.E,
        [UIKeyboardHidUsage.KeyboardF] = PhysicalKey.F,
        [UIKeyboardHidUsage.KeyboardG] = PhysicalKey.G,
        [UIKeyboardHidUsage.KeyboardH] = PhysicalKey.H,
        [UIKeyboardHidUsage.KeyboardI] = PhysicalKey.I,
        [UIKeyboardHidUsage.KeyboardJ] = PhysicalKey.J,
        [UIKeyboardHidUsage.KeyboardK] = PhysicalKey.K,
        [UIKeyboardHidUsage.KeyboardL] = PhysicalKey.L,
        [UIKeyboardHidUsage.KeyboardM] = PhysicalKey.M,
        [UIKeyboardHidUsage.KeyboardN] = PhysicalKey.N,
        [UIKeyboardHidUsage.KeyboardO] = PhysicalKey.O,
        [UIKeyboardHidUsage.KeyboardP] = PhysicalKey.P,
        [UIKeyboardHidUsage.KeyboardQ] = PhysicalKey.Q,
        [UIKeyboardHidUsage.KeyboardR] = PhysicalKey.R,
        [UIKeyboardHidUsage.KeyboardS] = PhysicalKey.S,
        [UIKeyboardHidUsage.KeyboardT] = PhysicalKey.T,
        [UIKeyboardHidUsage.KeyboardU] = PhysicalKey.U,
        [UIKeyboardHidUsage.KeyboardV] = PhysicalKey.V,
        [UIKeyboardHidUsage.KeyboardW] = PhysicalKey.W,
        [UIKeyboardHidUsage.KeyboardX] = PhysicalKey.X,
        [UIKeyboardHidUsage.KeyboardY] = PhysicalKey.Y,
        [UIKeyboardHidUsage.KeyboardZ] = PhysicalKey.Z,
        [UIKeyboardHidUsage.Keyboard1] = PhysicalKey.Digit1,
        [UIKeyboardHidUsage.Keyboard2] = PhysicalKey.Digit2,
        [UIKeyboardHidUsage.Keyboard3] = PhysicalKey.Digit3,
        [UIKeyboardHidUsage.Keyboard4] = PhysicalKey.Digit4,
        [UIKeyboardHidUsage.Keyboard5] = PhysicalKey.Digit5,
        [UIKeyboardHidUsage.Keyboard6] = PhysicalKey.Digit6,
        [UIKeyboardHidUsage.Keyboard7] = PhysicalKey.Digit7,
        [UIKeyboardHidUsage.Keyboard8] = PhysicalKey.Digit8,
        [UIKeyboardHidUsage.Keyboard9] = PhysicalKey.Digit9,
        [UIKeyboardHidUsage.Keyboard0] = PhysicalKey.Digit0,
        [UIKeyboardHidUsage.KeyboardReturnOrEnter] = PhysicalKey.Enter,
        [UIKeyboardHidUsage.KeyboardEscape] = PhysicalKey.Escape,
        [UIKeyboardHidUsage.KeyboardDeleteOrBackspace] = PhysicalKey.Delete,
        [UIKeyboardHidUsage.KeyboardTab] = PhysicalKey.Tab,
        [UIKeyboardHidUsage.KeyboardSpacebar] = PhysicalKey.Space,
        [UIKeyboardHidUsage.KeyboardHyphen] = PhysicalKey.NumPadSubtract,
        [UIKeyboardHidUsage.KeyboardEqualSign] = PhysicalKey.NumPadEqual,
        [UIKeyboardHidUsage.KeyboardOpenBracket] = PhysicalKey.BracketLeft,
        [UIKeyboardHidUsage.KeyboardCloseBracket] = PhysicalKey.BracketRight,
        [UIKeyboardHidUsage.KeyboardBackslash] = PhysicalKey.Backslash,
        // [UIKeyboardHidUsage.KeyboardNonUSPound] = 50,
        [UIKeyboardHidUsage.KeyboardSemicolon] = PhysicalKey.Semicolon,
        [UIKeyboardHidUsage.KeyboardQuote] = PhysicalKey.Quote,
        // [UIKeyboardHidUsage.KeyboardGraveAccentAndTilde] = 53,
        [UIKeyboardHidUsage.KeyboardComma] = PhysicalKey.Comma,
        [UIKeyboardHidUsage.KeyboardPeriod] = PhysicalKey.Period,
        [UIKeyboardHidUsage.KeyboardSlash] = PhysicalKey.Slash,
        [UIKeyboardHidUsage.KeyboardCapsLock] = PhysicalKey.CapsLock,
        [UIKeyboardHidUsage.KeyboardF1] = PhysicalKey.F1,
        [UIKeyboardHidUsage.KeyboardF2] = PhysicalKey.F2,
        [UIKeyboardHidUsage.KeyboardF3] = PhysicalKey.F3,
        [UIKeyboardHidUsage.KeyboardF4] = PhysicalKey.F4,
        [UIKeyboardHidUsage.KeyboardF5] = PhysicalKey.F5,
        [UIKeyboardHidUsage.KeyboardF6] = PhysicalKey.F6,
        [UIKeyboardHidUsage.KeyboardF7] = PhysicalKey.F7,
        [UIKeyboardHidUsage.KeyboardF8] = PhysicalKey.F8,
        [UIKeyboardHidUsage.KeyboardF9] = PhysicalKey.F9,
        [UIKeyboardHidUsage.KeyboardF10] = PhysicalKey.F10,
        [UIKeyboardHidUsage.KeyboardF11] = PhysicalKey.F11,
        [UIKeyboardHidUsage.KeyboardF12] = PhysicalKey.F12,
        [UIKeyboardHidUsage.KeyboardPrintScreen] = PhysicalKey.PrintScreen,
        [UIKeyboardHidUsage.KeyboardScrollLock] = PhysicalKey.ScrollLock,
        [UIKeyboardHidUsage.KeyboardPause] = PhysicalKey.Pause,
        [UIKeyboardHidUsage.KeyboardInsert] = PhysicalKey.Insert,
        [UIKeyboardHidUsage.KeyboardHome] = PhysicalKey.Home,
        [UIKeyboardHidUsage.KeyboardPageUp] = PhysicalKey.PageUp,
        [UIKeyboardHidUsage.KeyboardDeleteForward] = PhysicalKey.Delete,
        [UIKeyboardHidUsage.KeyboardEnd] = PhysicalKey.End,
        [UIKeyboardHidUsage.KeyboardPageDown] = PhysicalKey.PageDown,
        [UIKeyboardHidUsage.KeyboardRightArrow] = PhysicalKey.ArrowRight,
        [UIKeyboardHidUsage.KeyboardLeftArrow] = PhysicalKey.ArrowLeft,
        [UIKeyboardHidUsage.KeyboardDownArrow] = PhysicalKey.ArrowDown,
        [UIKeyboardHidUsage.KeyboardUpArrow] = PhysicalKey.ArrowUp,
        [UIKeyboardHidUsage.KeypadNumLock] = PhysicalKey.NumLock,
        [UIKeyboardHidUsage.KeypadSlash] = PhysicalKey.Slash,
        [UIKeyboardHidUsage.KeypadAsterisk] = PhysicalKey.NumPadMultiply,
        [UIKeyboardHidUsage.KeypadHyphen] = PhysicalKey.NumPadSubtract,
        [UIKeyboardHidUsage.KeypadPlus] = PhysicalKey.NumPadAdd,
        [UIKeyboardHidUsage.KeypadEnter] = PhysicalKey.Enter,
        [UIKeyboardHidUsage.Keypad1] = PhysicalKey.NumPad1,
        [UIKeyboardHidUsage.Keypad2] = PhysicalKey.NumPad2,
        [UIKeyboardHidUsage.Keypad3] = PhysicalKey.NumPad3,
        [UIKeyboardHidUsage.Keypad4] = PhysicalKey.NumPad4,
        [UIKeyboardHidUsage.Keypad5] = PhysicalKey.NumPad5,
        [UIKeyboardHidUsage.Keypad6] = PhysicalKey.NumPad6,
        [UIKeyboardHidUsage.Keypad7] = PhysicalKey.NumPad7,
        [UIKeyboardHidUsage.Keypad8] = PhysicalKey.NumPad8,
        [UIKeyboardHidUsage.Keypad9] = PhysicalKey.NumPad9,
        [UIKeyboardHidUsage.Keypad0] = PhysicalKey.NumPad0,
        [UIKeyboardHidUsage.KeypadPeriod] = PhysicalKey.Period,
        [UIKeyboardHidUsage.KeyboardNonUSBackslash] = PhysicalKey.IntlBackslash,
        //[UIKeyboardHidUsage.KeyboardApplication] = 101,
        //[UIKeyboardHidUsage.KeyboardPower] = 102,
        //[UIKeyboardHidUsage.KeypadEqualSign] = 103,
        [UIKeyboardHidUsage.KeyboardF13] = PhysicalKey.F13,
        [UIKeyboardHidUsage.KeyboardF14] = PhysicalKey.F14,
        [UIKeyboardHidUsage.KeyboardF15] = PhysicalKey.F15,
        [UIKeyboardHidUsage.KeyboardF16] = PhysicalKey.F16,
        [UIKeyboardHidUsage.KeyboardF17] = PhysicalKey.F17,
        [UIKeyboardHidUsage.KeyboardF18] = PhysicalKey.F18,
        [UIKeyboardHidUsage.KeyboardF19] = PhysicalKey.F19,
        [UIKeyboardHidUsage.KeyboardF20] = PhysicalKey.F20,
        [UIKeyboardHidUsage.KeyboardF21] = PhysicalKey.F21,
        [UIKeyboardHidUsage.KeyboardF22] = PhysicalKey.F22,
        [UIKeyboardHidUsage.KeyboardF23] = PhysicalKey.F23,
        [UIKeyboardHidUsage.KeyboardF24] = PhysicalKey.F24,
        //[UIKeyboardHidUsage.KeyboardExecute] = 116,
        //[UIKeyboardHidUsage.KeyboardHelp] = 117,
        //[UIKeyboardHidUsage.KeyboardMenu] = 118,
        [UIKeyboardHidUsage.KeyboardSelect] = PhysicalKey.Space,
        //[UIKeyboardHidUsage.KeyboardStop] = 120,
        //[UIKeyboardHidUsage.KeyboardAgain] = 121,
        //[UIKeyboardHidUsage.KeyboardUndo] = 122,
        //[UIKeyboardHidUsage.KeyboardCut] = 123,
        //[UIKeyboardHidUsage.KeyboardCopy] = 124,
        //[UIKeyboardHidUsage.KeyboardPaste] = 125,
        //[UIKeyboardHidUsage.KeyboardFind] = 126,
        [UIKeyboardHidUsage.KeyboardMute] = PhysicalKey.AudioVolumeMute,
        [UIKeyboardHidUsage.KeyboardVolumeUp] = PhysicalKey.AudioVolumeUp,
        [UIKeyboardHidUsage.KeyboardVolumeDown] = PhysicalKey.AudioVolumeDown,
        //[UIKeyboardHidUsage.KeyboardLockingCapsLock] = PhysicalKey.CapsLock,
        //[UIKeyboardHidUsage.KeyboardLockingNumLock] = PhysicalKey.Space,
        //[UIKeyboardHidUsage.KeyboardLockingScrollLock] = 132,
        [UIKeyboardHidUsage.KeypadComma] = PhysicalKey.NumPadComma,
        //[UIKeyboardHidUsage.KeypadEqualSignAS400] = 134,
        //[UIKeyboardHidUsage.KeyboardInternational1] = 135,
        //[UIKeyboardHidUsage.KeyboardInternational2] = 136,
        //[UIKeyboardHidUsage.KeyboardInternational3] = 137,
        //[UIKeyboardHidUsage.KeyboardInternational4] = 138,
        //[UIKeyboardHidUsage.KeyboardInternational5] = 139,
        //[UIKeyboardHidUsage.KeyboardInternational6] = 140,
        //[UIKeyboardHidUsage.KeyboardInternational7] = 141,
        //[UIKeyboardHidUsage.KeyboardInternational8] = 142,
        //[UIKeyboardHidUsage.KeyboardInternational9] = 143,
        //[UIKeyboardHidUsage.KeyboardHangul] = 144,
        //[UIKeyboardHidUsage.KeyboardKanaSwitch] = 144,
        //[UIKeyboardHidUsage.KeyboardLang1] = 144,
        //[UIKeyboardHidUsage.KeyboardAlphanumericSwitch] = 145,
        //[UIKeyboardHidUsage.KeyboardHanja] = 145,
        //[UIKeyboardHidUsage.KeyboardLang2] = 145,
        //[UIKeyboardHidUsage.KeyboardKatakana] = 146,
        //[UIKeyboardHidUsage.KeyboardLang3] = 146,
        //[UIKeyboardHidUsage.KeyboardHiragana] = 147,
        //[UIKeyboardHidUsage.KeyboardLang4] = 147,
        //[UIKeyboardHidUsage.KeyboardLang5] = 148,
        //[UIKeyboardHidUsage.KeyboardZenkakuHankakuKanji] = 148,
        //[UIKeyboardHidUsage.KeyboardLang6] = 149,
        //[UIKeyboardHidUsage.KeyboardLang7] = 150,
        //[UIKeyboardHidUsage.KeyboardLang8] = 151,
        //[UIKeyboardHidUsage.KeyboardLang9] = 152,
        //[UIKeyboardHidUsage.KeyboardAlternateErase] = 153,
        //[UIKeyboardHidUsage.KeyboardSysReqOrAttention] = 154,
        //[UIKeyboardHidUsage.KeyboardCancel] = PhysicalKey.Cancel,
        //[UIKeyboardHidUsage.KeyboardClear] = PhysicalKey.NumPadClear,
        //[UIKeyboardHidUsage.KeyboardPrior] = PhysicalKey.Prior,
        //[UIKeyboardHidUsage.KeyboardReturn] = PhysicalKey.Return,
        //[UIKeyboardHidUsage.KeyboardSeparator] = PhysicalKey.Separator,
        //[UIKeyboardHidUsage.KeyboardOut] = 160,
        //[UIKeyboardHidUsage.KeyboardOper] = 161,
        //[UIKeyboardHidUsage.KeyboardClearOrAgain] = 162,
        //[UIKeyboardHidUsage.KeyboardCrSelOrProps] = 163,
        //[UIKeyboardHidUsage.KeyboardExSel] = 164,
        [UIKeyboardHidUsage.KeyboardLeftControl] = PhysicalKey.ControlLeft,
        [UIKeyboardHidUsage.KeyboardLeftShift] = PhysicalKey.ShiftLeft,
        [UIKeyboardHidUsage.KeyboardLeftAlt] = PhysicalKey.AltLeft,
        [UIKeyboardHidUsage.KeyboardLeftGui] = PhysicalKey.MetaLeft,
        [UIKeyboardHidUsage.KeyboardRightControl] = PhysicalKey.ControlRight,
        [UIKeyboardHidUsage.KeyboardRightShift] = PhysicalKey.ShiftRight,
        [UIKeyboardHidUsage.KeyboardRightAlt] = PhysicalKey.AltRight,
        [UIKeyboardHidUsage.KeyboardRightGui] = PhysicalKey.MetaRight,
        //[UIKeyboardHidUsage.KeyboardReserved] = 65535,
    };
#pragma warning restore CA1416
}
