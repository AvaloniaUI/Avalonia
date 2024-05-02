#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.FreeDesktop;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Platform.Interop;
using static Avalonia.X11.XLib;

namespace Avalonia.X11
{
    internal partial class X11Window
    {
        private ITextInputMethodImpl? _ime;
        private IX11InputMethodControl? _imeControl;
        private bool _processingIme;

        private readonly Queue<(RawKeyEventArgs args, XEvent xev, int keyval, int keycode)> _imeQueue = new();

        private unsafe void CreateIC()
        {
            if (_x11.HasXim)
            {
                XGetIMValues(_x11.Xim, XNames.XNQueryInputStyle, out var supported_styles, IntPtr.Zero);
                for (var c = 0; c < supported_styles->count_styles; c++)
                {
                    var style = (XIMProperties)supported_styles->supported_styles[c];
                    if ((int)(style & XIMProperties.XIMPreeditPosition) != 0
                        && ((int)(style & XIMProperties.XIMStatusNothing) != 0))
                    {
                        XPoint spot = default;

                        //using var areaS = new Utf8Buffer("area");
                        using var spotS = new Utf8Buffer("spotLocation");
                        using var fontS = new Utf8Buffer("fontSet");

                        var list = XVaCreateNestedList(0,
                            //areaS, &area,
                            spotS, &spot,
                            fontS, _x11.DefaultFontSet,
                            IntPtr.Zero);
                        _xic = XCreateIC(_x11.Xim,
                            XNames.XNClientWindow, _handle,
                            XNames.XNFocusWindow, _handle,
                            XNames.XNInputStyle, new IntPtr((int)style),
                            XNames.XNResourceName, _platform.Options.WmClass,
                            XNames.XNResourceClass, _platform.Options.WmClass,
                            XNames.XNPreeditAttributes, list,
                            IntPtr.Zero);

                        XFree(list);

                        break;
                    }
                }
                
                XFree(new IntPtr(supported_styles));
            }
            
            if (_xic == IntPtr.Zero)
                _xic = XCreateIC(_x11.Xim, XNames.XNInputStyle,
                    new IntPtr((int)(XIMProperties.XIMPreeditNothing | XIMProperties.XIMStatusNothing)),
                    XNames.XNClientWindow, _handle, XNames.XNFocusWindow, _handle, IntPtr.Zero);
        }

        private void InitializeIme()
        {
            var ime =  AvaloniaLocator.Current.GetService<IX11InputMethodFactory>()?.CreateClient(_handle);
            if (ime == null && _x11.HasXim)
            {
                var xim = new XimInputMethod(this);
                ime = (xim, xim);
            }
            if (ime != null)
            {
                (_ime, _imeControl) = ime.Value;
                _imeControl.Commit += s =>
                    ScheduleInput(new RawTextInputEventArgs(_keyboard, (ulong)_x11.LastActivityTimestamp.ToInt64(),
                        InputRoot, s));
                _imeControl.ForwardKey += OnImeControlForwardKey;
            }
        }

        private void OnImeControlForwardKey(X11InputMethodForwardedKey forwardedKey)
        {
            var x11Key = (X11Key)forwardedKey.KeyVal;
            var keySymbol = _x11.HasXkb ? GetKeySymbolXkb(x11Key) : GetKeySymbolXCore(x11Key);

            ScheduleInput(new RawKeyEventArgs(
                _keyboard,
                (ulong)_x11.LastActivityTimestamp.ToInt64(),
                InputRoot,
                forwardedKey.Type,
                X11KeyTransform.KeyFromX11Key(x11Key),
                (RawInputModifiers)forwardedKey.Modifiers,
                PhysicalKey.None,
                keySymbol));
        }

        private void UpdateImePosition() => _imeControl?.UpdateWindowInfo(_position ?? default, RenderScaling);

        private void HandleKeyEvent(ref XEvent ev)
        {
            var physicalKey = X11KeyTransform.PhysicalKeyFromScanCode(ev.KeyEvent.keycode);
            var (x11Key, key, symbol) = LookupKey(ref ev.KeyEvent, physicalKey);
            var modifiers = TranslateModifiers(ev.KeyEvent.state);
            var timestamp = (ulong)ev.KeyEvent.time.ToInt64();

            var args = ev.type == XEventName.KeyPress ?
                new RawKeyEventArgsWithText(
                    _keyboard,
                    timestamp,
                    InputRoot,
                    RawKeyEventType.KeyDown,
                    key,
                    modifiers,
                    physicalKey,
                    symbol,
                    TranslateEventToString(ref ev, symbol)) :
                new RawKeyEventArgs(
                    _keyboard,
                    timestamp,
                    InputRoot,
                    RawKeyEventType.KeyUp,
                    key,
                    modifiers,
                    physicalKey,
                    symbol);

            ScheduleKeyInput(args, ref ev, (int)x11Key, ev.KeyEvent.keycode);
        }

        private (X11Key x11Key, Key key, string? symbol) LookupKey(ref XKeyEvent keyEvent, PhysicalKey physicalKey)
        {
            var (x11Key, key, symbol) = _x11.HasXkb ? LookUpKeyXkb(ref keyEvent) : LookupKeyXCore(ref keyEvent);

            // Always use digits keys if possible, matching Windows/macOS.
            if (physicalKey is >= PhysicalKey.Digit0 and <= PhysicalKey.Digit9)
                key = physicalKey.ToQwertyKey();

            // No key sym matched a key (e.g. non-latin keyboard without US fallback): fallback to a basic QWERTY map.
            if (x11Key != 0 && key == Key.None)
                key = physicalKey.ToQwertyKey();

            return (x11Key, key, symbol);
        }

        private (X11Key x11Key, Key key, string? symbol) LookUpKeyXkb(ref XKeyEvent keyEvent)
        {
            // First lookup using the current keyboard layout group (contained in state).
            var state = (int)keyEvent.state;
            if (!XkbLookupKeySym(_x11.Display, keyEvent.keycode, state, out _, out var originalKeySym))
                return (0, Key.None, null);

            var x11Key = (X11Key)originalKeySym;
            var symbol = GetKeySymbolXkb(x11Key);

            var key = X11KeyTransform.KeyFromX11Key(x11Key);
            if (key != Key.None)
                return (x11Key, key, symbol);

            var originalGroup = XkbGetGroupForCoreState(state);

            // We got a KeySym that doesn't match a key: try the other groups.
            // This is needed to get a latin key for non-latin keyboard layouts.
            for (var group = 0; group < 4; ++group)
            {
                if (group == originalGroup)
                    continue;

                var newState = XkbSetGroupForCoreState(state, group);
                if (XkbLookupKeySym(_x11.Display, keyEvent.keycode, newState, out _, out var groupKeySym))
                {
                    key = X11KeyTransform.KeyFromX11Key((X11Key)groupKeySym);
                    if (key != Key.None)
                        return (x11Key, key, symbol);
                }
            }

            return (x11Key, Key.None, null);
        }

        private unsafe string? GetKeySymbolXkb(X11Key x11Key)
        {
            var keySym = (nint)x11Key;
            const int bufferSize = 4;
            var buffer = stackalloc byte[bufferSize];
            var length = XkbTranslateKeySym(_x11.Display, ref keySym, 0, buffer, bufferSize, out var extraSize);

            if (length == 0)
                return null;

            if (length == 1 && !KeySymbolHelper.IsAllowedAsciiKeySymbol((char)buffer[0]))
                return null;

            if (extraSize <= 0)
                return Encoding.UTF8.GetString(buffer, length);

            // A symbol should normally fit in 4 bytes, so this path isn't expected to be taken.
            var heapBuffer = new byte[length + extraSize];
            fixed (byte* heapBufferPtr = heapBuffer)
                length = XkbTranslateKeySym(_x11.Display, ref keySym, 0, heapBufferPtr, heapBuffer.Length, out _);

            return Encoding.UTF8.GetString(heapBuffer, 0, length);
        }

        private static unsafe (X11Key x11Key, Key key, string? symbol) LookupKeyXCore(ref XKeyEvent keyEvent)
        {
            const int bufferSize = 4;
            var buffer = stackalloc byte[bufferSize];

            // We don't have Xkb enabled, which should be rare: use XLookupString which will map to the first keyboard
            // while handling modifiers for us (XKeycodeToKeysym doesn't).
            var length = XLookupString(ref keyEvent, buffer, bufferSize, out var keySym, IntPtr.Zero);

            var x11Key = (X11Key)keySym;
            var key = X11KeyTransform.KeyFromX11Key(x11Key);

            var symbol = length switch
            {
                0 => null,
                1 when !KeySymbolHelper.IsAllowedAsciiKeySymbol((char)buffer[0]) => null,
                _ => Encoding.UTF8.GetString(buffer, length)
            };

            return (x11Key, key, symbol);
        }

        private static unsafe string? GetKeySymbolXCore(X11Key x11Key)
        {
            var bytes = XKeysymToString((nint)x11Key);
            if (bytes is null)
                return null;

            var length = 0;
            for (var p = bytes; *p != 0; ++p)
                ++length;

            if (length == 0)
                return null;

            return Encoding.UTF8.GetString(bytes, length);
        }

        private const int ImeBufferSize = 64 * 1024;
        [ThreadStatic] private static IntPtr ImeBuffer;

        private unsafe string? TranslateEventToString(ref XEvent ev, string? symbol)
        {
            string? text;

            if (!_x11.HasXkb && _xic == IntPtr.Zero)
                text = symbol; // We already got the symbol from XLookupString, no need to call it again.
            else
            {
                if (ImeBuffer == IntPtr.Zero)
                    ImeBuffer = Marshal.AllocHGlobal(ImeBufferSize);

                var imeBufferPtr = (byte*)ImeBuffer.ToPointer();
                XLookupStatus status = 0;

                var len = _xic == IntPtr.Zero ?
                    XLookupString(ref ev.KeyEvent, imeBufferPtr, ImeBufferSize, out _, IntPtr.Zero) :
                    Xutf8LookupString(_xic, ref ev.KeyEvent, imeBufferPtr, ImeBufferSize, out _, out status);

                if (len == 0 || status == XLookupStatus.XBufferOverflow)
                    return null;

                text = Encoding.UTF8.GetString(imeBufferPtr, len);
            }

            if (text is null)
                return null;

            if (text.Length == 1)
            {
                if (text[0] < ' ' || text[0] == 0x7f) // Control codes or DEL
                    return null;
            }

            return text;
        }

        private void ScheduleKeyInput(RawKeyEventArgs args, ref XEvent xev, int keyval, int keycode)
        {
            _x11.LastActivityTimestamp = xev.ButtonEvent.time;
            
            if (_imeControl is { IsEnabled: true } 
                && FilterIme(args, xev, keyval, keycode)) 
                return;
            
            ScheduleInput(args);
        }

        private bool FilterIme(RawKeyEventArgs args, XEvent xev, int keyval, int keycode)
        {
            if (_ime == null)
                return false;
            _imeQueue.Enqueue((args, xev, keyval, keycode));
            if (!_processingIme)
                ProcessNextImeEvent();

            return true;
        }

        private async void ProcessNextImeEvent()
        {
            if(_processingIme)
                return;
            _processingIme = true;
            try
            {
                while (_imeQueue.Count != 0)
                {
                    var ev = _imeQueue.Dequeue();
                    if (_imeControl != null)
                    {
                        var handledByIme = await _imeControl.HandleEventAsync(ev.args, ev.keyval, ev.keycode);
                        if (handledByIme && ev.args is not
                            {
                                // We let filtered modifier-key KeyUp events through
                                // since some apps rely on the order of events to track individual (left/right)
                                // modifier keys states rather than relying on general key modifiers
                                Type: RawKeyEventType.KeyUp,
                                Key: Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt
                                or Key.RightAlt or Key.LeftShift or Key.RightShift
                                or Key.LWin or Key.RWin
                            })
                            continue;

                    }

                    ScheduleInput(ev.args);

                }
            }
            finally
            {
                _processingIme = false;
            }
        }

        // This class is used to attach the text value of the key to an asynchronously dispatched KeyDown event
        private class RawKeyEventArgsWithText : RawKeyEventArgs
        {
            public RawKeyEventArgsWithText(
                IKeyboardDevice device,
                ulong timestamp,
                IInputRoot root,
                RawKeyEventType type,
                Key key,
                RawInputModifiers modifiers,
                PhysicalKey physicalKey,
                string? keySymbol,
                string? text)
                : base(device, timestamp, root, type, key, modifiers, physicalKey, keySymbol)
            {
                Text = text;
            }

            public string? Text { get; }
        }
    }
}
