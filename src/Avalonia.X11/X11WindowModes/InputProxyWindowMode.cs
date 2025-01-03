using System;
using System.Collections.Generic;

namespace Avalonia.X11;
using static XLib;

partial class X11Window
{
    public class InputProxyWindowMode : DefaultTopLevelWindowMode
    {
        private X11FocusProxy _focusProxy;

        public override void OnHandleCreated(IntPtr handle)
        {
            _focusProxy = new X11FocusProxy(Platform, handle, OnFocusProxyEvent);
            Window.SetWmClass(_focusProxy._handle, "FocusProxy");
            base.OnHandleCreated(handle);
        }

        public override bool OnEvent(ref XEvent ev)
        {
            if (ev.type == XEventName.ClientMessage && ev.ClientMessageEvent.ptr1 == X11.Atoms.WM_TAKE_FOCUS)
            {
                XSetInputFocus(X11.Display, _focusProxy!._handle, RevertTo.Parent, ev.ClientMessageEvent.ptr2);
            }
            return base.OnEvent(ref ev);
        }

        void OnFocusProxyEvent(ref XEvent xev)
        {
            
        }

        protected override void OnManualXRaiseWindow()
        {
            if (_focusProxy is not null)
                XSetInputFocus(X11.Display, _focusProxy._handle, 0, IntPtr.Zero);
            base.OnManualXRaiseWindow();
        }


        public override void OnDestroyNotify()
        {
            _focusProxy?.Cleanup();
            _focusProxy = null;
            base.OnDestroyNotify();
        }

        public override void AppendWmProtocols(List<IntPtr> data)
        {
            data.Add(X11.Atoms.WM_TAKE_FOCUS);
            base.AppendWmProtocols(data);
        }
    }
}