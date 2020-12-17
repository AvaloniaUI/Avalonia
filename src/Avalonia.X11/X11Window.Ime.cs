using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Avalonia.FreeDesktop;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using static Avalonia.X11.XLib;

namespace Avalonia.X11
{
    partial class X11Window
    {
        private ITextInputMethodImpl _ime;
        private IX11InputMethodControl _imeControl;
        private bool _processingIme;

        private Queue<(RawKeyEventArgs args, XEvent xev, int keyval, int keycode)> _imeQueue =
            new Queue<(RawKeyEventArgs args, XEvent xev, int keyVal, int keyCode)>();

        void InitializeIme()
        {
            var ime =  AvaloniaLocator.Current.GetService<IX11InputMethodFactory>()?.CreateClient(_handle);
            if (ime != null)
            {
                (_ime, _imeControl) = ime.Value;
                _imeControl.OnCommit += s =>
                    ScheduleInput(new RawTextInputEventArgs(_keyboard, (ulong)_x11.LastActivityTimestamp.ToInt64(),
                        _inputRoot, s));
                _imeControl.OnForwardKey += ev =>
                {
                    ScheduleInput(new RawKeyEventArgs(_keyboard, (ulong)_x11.LastActivityTimestamp.ToInt64(),
                        _inputRoot, ev.Type, X11KeyTransform.ConvertKey((X11Key)ev.KeyVal),
                        (RawInputModifiers)ev.Modifiers));
                };
            }
        }

        void UpdateImePosition() => _imeControl?.UpdateWindowInfo(Position, RenderScaling);

        async void HandleKeyEvent(XEvent ev)
        {
            

            var index = ev.KeyEvent.state.HasFlag(XModifierMask.ShiftMask);
                
            // We need the latin key, since it's mainly used for hotkeys, we use a different API for text anyway
            var key = (X11Key)XKeycodeToKeysym(_x11.Display, ev.KeyEvent.keycode, index ? 1 : 0).ToInt32();
                
            // Manually switch the Shift index for the keypad,
            // there should be a proper way to do this
            if (ev.KeyEvent.state.HasFlag(XModifierMask.Mod2Mask)
                && key > X11Key.Num_Lock && key <= X11Key.KP_9)
                key = (X11Key)XKeycodeToKeysym(_x11.Display, ev.KeyEvent.keycode, index ? 0 : 1).ToInt32();
            
            var filtered = ScheduleKeyInput(new RawKeyEventArgs(_keyboard, (ulong)ev.KeyEvent.time.ToInt64(), _inputRoot,
                ev.type == XEventName.KeyPress ? RawKeyEventType.KeyDown : RawKeyEventType.KeyUp,
                X11KeyTransform.ConvertKey(key), TranslateModifiers(ev.KeyEvent.state)), ref ev, (int)key, ev.KeyEvent.keycode);

            if (_handle == IntPtr.Zero)
                return;
            
            if (ev.type == XEventName.KeyPress && !filtered) 
                TriggerClassicTextInputEvent(ev);
        }

        void TriggerClassicTextInputEvent(XEvent ev)
        {
            var text = TranslateEventToString(ev);
            if (text != null)
                ScheduleInput(
                    new RawTextInputEventArgs(_keyboard, (ulong)ev.KeyEvent.time.ToInt64(), _inputRoot, text),
                    ref ev);
        }

        unsafe string TranslateEventToString(XEvent ev)
        {
            var buffer = stackalloc byte[40];
            var len = Xutf8LookupString(_xic, ref ev, buffer, 40, out _, out _);
            if (len != 0)
            {
                var text = Encoding.UTF8.GetString(buffer, len);
                if (text.Length == 1)
                {
                    if (text[0] < ' ' || text[0] == 0x7f) //Control codes or DEL
                        return null;
                }

                return text;
            }

            return null;
        }
        
        
        bool ScheduleKeyInput(RawKeyEventArgs args, ref XEvent xev, int keyval, int keycode)
        {
            _x11.LastActivityTimestamp = xev.ButtonEvent.time;
            if (_imeControl != null && _imeControl.IsEnabled)
            {
                if (FilterIme(args, xev, keyval, keycode))
                    return true;
            }
            ScheduleInput(args);
            return false;
        }
        
        bool FilterIme(RawKeyEventArgs args, XEvent xev, int keyval, int keycode)
        {
            if (_ime == null)
                return false;
            _imeQueue.Enqueue((args, xev, keyval, keycode));
            if (!_processingIme)
                ProcessNextImeEvent();

            return true;
        }

        async void ProcessNextImeEvent()
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
                    {
                        ScheduleInput(ev.args);
                        if (ev.args.Type == RawKeyEventType.KeyDown)
                            TriggerClassicTextInputEvent(ev.xev);
                    }
                }
            }
            finally
            {
                _processingIme = false;
            }
        }
    }
}
