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
        private ITextInputMethodImpl _ime;
        private IX11InputMethodControl _imeControl;
        private bool _processingIme;

        private Queue<(RawKeyEventArgs args, XEvent xev, int keyval, int keycode)> _imeQueue =
            new Queue<(RawKeyEventArgs args, XEvent xev, int keyVal, int keyCode)>();

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
                        _inputRoot, s));
                _imeControl.ForwardKey += ev =>
                {
                    ScheduleInput(new RawKeyEventArgs(_keyboard, (ulong)_x11.LastActivityTimestamp.ToInt64(),
                        _inputRoot, ev.Type, X11KeyTransform.ConvertKey((X11Key)ev.KeyVal),
                        (RawInputModifiers)ev.Modifiers));
                };
            }
        }

        private void UpdateImePosition() => _imeControl?.UpdateWindowInfo(Position, RenderScaling);

        private void HandleKeyEvent(ref XEvent ev)
        {
            var index = ev.KeyEvent.state.HasAllFlags(XModifierMask.ShiftMask);

            // We need the latin key, since it's mainly used for hotkeys, we use a different API for text anyway
            var key = (X11Key)XKeycodeToKeysym(_x11.Display, ev.KeyEvent.keycode, index ? 1 : 0).ToInt32();
                
            // Manually switch the Shift index for the keypad,
            // there should be a proper way to do this
            if (ev.KeyEvent.state.HasAllFlags(XModifierMask.Mod2Mask)
                && key > X11Key.Num_Lock && key <= X11Key.KP_9)
                key = (X11Key)XKeycodeToKeysym(_x11.Display, ev.KeyEvent.keycode, index ? 0 : 1).ToInt32();

            var convertedKey = X11KeyTransform.ConvertKey(key);
            var modifiers = TranslateModifiers(ev.KeyEvent.state);
            var timestamp = (ulong)ev.KeyEvent.time.ToInt64();
            RawKeyEventArgs args =
                ev.type == XEventName.KeyPress
                    ? new RawKeyEventArgsWithText(_keyboard, timestamp, _inputRoot, RawKeyEventType.KeyDown,
                        convertedKey, modifiers, TranslateEventToString(ref ev))
                    : new RawKeyEventArgs(_keyboard, timestamp, _inputRoot, RawKeyEventType.KeyUp, convertedKey,
                        modifiers);

            ScheduleKeyInput(args, ref ev, (int)key, ev.KeyEvent.keycode);
        }

        private void TriggerClassicTextInputEvent(ref XEvent ev)
        {
            var text = TranslateEventToString(ref ev);
            if (text != null)
                ScheduleInput(
                    new RawTextInputEventArgs(_keyboard, (ulong)ev.KeyEvent.time.ToInt64(), _inputRoot, text),
                    ref ev);
        }

        private const int ImeBufferSize = 64 * 1024;
        [ThreadStatic] private static IntPtr ImeBuffer;

        private unsafe string TranslateEventToString(ref XEvent ev)
        {
            if (ImeBuffer == IntPtr.Zero)
                ImeBuffer = Marshal.AllocHGlobal(ImeBufferSize);
            
            var len = Xutf8LookupString(_xic, ref ev, ImeBuffer.ToPointer(), ImeBufferSize, 
                out _, out var istatus);
            var status = (XLookupStatus)istatus;

            if (len == 0)
                return null;

            string text;
            if (status == XLookupStatus.XBufferOverflow)
                return null;
            else
                text = Encoding.UTF8.GetString((byte*)ImeBuffer.ToPointer(), len);

            if (text == null)
                return null;
            
            if (text.Length == 1)
            {
                if (text[0] < ' ' || text[0] == 0x7f) //Control codes or DEL
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
                    if (_imeControl == null || !await _imeControl.HandleEventAsync(ev.args, ev.keyval, ev.keycode))
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
            public RawKeyEventArgsWithText(IKeyboardDevice device, ulong timestamp, IInputRoot root,
                RawKeyEventType type, Key key, RawInputModifiers modifiers, string text) :
                base(device, timestamp, root, type, key, modifiers)
            {
                Text = text;
            }
            
            public string Text { get; }
        }
    }
}
