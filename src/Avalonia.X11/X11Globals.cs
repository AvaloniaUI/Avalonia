using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using static Avalonia.X11.XLib;
namespace Avalonia.X11
{
    unsafe class X11Globals
    {
        private readonly AvaloniaX11Platform _plat;
        private readonly int _screenNumber;
        private readonly X11Info _x11;
        private readonly IntPtr _rootWindow;
        private readonly IntPtr _compositingAtom;
        private readonly List<IGlobalsSubscriber> _subscribers = new List<IGlobalsSubscriber>();
        
        private string _wmName;
        private IntPtr _compositionAtomOwner;
        private bool _isCompositionEnabled;

        public X11Globals(AvaloniaX11Platform plat)
        {
            _plat = plat;
            _x11 = plat.Info;
            _screenNumber = XDefaultScreen(_x11.Display);
            _rootWindow = XRootWindow(_x11.Display, _screenNumber);
            plat.Windows[_rootWindow] = OnRootWindowEvent;
            XSelectInput(_x11.Display, _rootWindow,
                new IntPtr((int)(EventMask.StructureNotifyMask | EventMask.PropertyChangeMask)));
            _compositingAtom = XInternAtom(_x11.Display, "_NET_WM_CM_S" + _screenNumber, false);
            UpdateWmName();
            UpdateCompositingAtomOwner();
        }
        
        public string WmName
        {
            get => _wmName;
            private set
            {
                if (_wmName != value)
                {
                    _wmName = value;
                    // The collection might change during enumeration
                    foreach (var s in _subscribers.ToArray()) 
                        s.WmChanged(value);
                }
            }
        }

        private IntPtr CompositionAtomOwner
        {
            get => _compositionAtomOwner;
            set
            {
                if (_compositionAtomOwner != value)
                {
                    _compositionAtomOwner = value;
                    IsCompositionEnabled = _compositionAtomOwner != IntPtr.Zero;
                }
            }
        }

        public bool IsCompositionEnabled
        {
            get => _isCompositionEnabled;
            set
            {
                if (_isCompositionEnabled != value)
                {
                    _isCompositionEnabled = value;
                    // The collection might change during enumeration
                    foreach (var s in _subscribers.ToArray()) 
                        s.CompositionChanged(value);
                }
            }
        }

        IntPtr GetSupportingWmCheck(IntPtr window)
        {
            XGetWindowProperty(_x11.Display, _rootWindow, _x11.Atoms._NET_SUPPORTING_WM_CHECK,
                IntPtr.Zero, new IntPtr(IntPtr.Size), false,
                _x11.Atoms.XA_WINDOW, out IntPtr actualType, out int actualFormat, out IntPtr nitems,
                out IntPtr bytesAfter, out IntPtr prop);
            if (nitems.ToInt32() != 1)
                return IntPtr.Zero;
            try
            {
                if (actualType != _x11.Atoms.XA_WINDOW)
                    return IntPtr.Zero;
                return *(IntPtr*)prop.ToPointer();
            }
            finally
            {
                XFree(prop);
            }
        }

        void UpdateCompositingAtomOwner()
        {
            // This procedure is described in https://tronche.com/gui/x/icccm/sec-2.html#s-2.8
            
            // Check the server-side selection owner
            var newOwner = XGetSelectionOwner(_x11.Display, _compositingAtom);
            while (CompositionAtomOwner != newOwner)
            {
                // We have a new owner, unsubscribe from the previous one first
                if (CompositionAtomOwner != IntPtr.Zero)
                {
                    _plat.Windows.Remove(CompositionAtomOwner);
                    XSelectInput(_x11.Display, CompositionAtomOwner, IntPtr.Zero);
                }

                // Set it as the current owner and select input
                CompositionAtomOwner = newOwner;
                if (CompositionAtomOwner != IntPtr.Zero)
                {
                    _plat.Windows[newOwner] = HandleCompositionAtomOwnerEvents;
                    XSelectInput(_x11.Display, CompositionAtomOwner, new IntPtr((int)(EventMask.StructureNotifyMask)));
                }
                
                // Check for the new owner again and repeat the procedure if it was changed between XGetSelectionOwner and XSelectInput call
                newOwner = XGetSelectionOwner(_x11.Display, _compositingAtom);
            }
        }

        private void HandleCompositionAtomOwnerEvents(ref XEvent ev)
        {
            if(ev.type == XEventName.DestroyNotify)
                UpdateCompositingAtomOwner();
        }
        
        void UpdateWmName() => WmName = GetWmName();

        string GetWmName()
        {
            var wm = GetSupportingWmCheck(_rootWindow);
            if (wm == IntPtr.Zero || wm != GetSupportingWmCheck(wm))
                return null;
            XGetWindowProperty(_x11.Display, wm, _x11.Atoms._NET_WM_NAME,
                IntPtr.Zero, new IntPtr(0x7fffffff),
                false, _x11.Atoms.UTF8_STRING, out var actualType, out var actualFormat,
                out var nitems, out _, out var prop);
            if (nitems == IntPtr.Zero)
                return null;
            try
            {
                if (actualFormat != 8)
                    return null;
                return Marshal.PtrToStringAnsi(prop, nitems.ToInt32());
            }
            finally
            {
                XFree(prop);
            }
        }
        
        private void OnRootWindowEvent(ref XEvent ev)
        {
            if (ev.type == XEventName.PropertyNotify)
            {
                if(ev.PropertyEvent.atom == _x11.Atoms._NET_SUPPORTING_WM_CHECK)
                    UpdateWmName();
            }

            if (ev.type == XEventName.ClientMessage)
            {
                if(ev.ClientMessageEvent.message_type == _x11.Atoms.MANAGER
                    && ev.ClientMessageEvent.ptr2 == _compositingAtom)
                    UpdateCompositingAtomOwner();
            }
        }
        
        public interface IGlobalsSubscriber
        {
            void WmChanged(string wmName);
            void CompositionChanged(bool compositing);
        }

        public void AddSubscriber(IGlobalsSubscriber subscriber) => _subscribers.Add(subscriber);
        public void RemoveSubscriber(IGlobalsSubscriber subscriber) => _subscribers.Remove(subscriber);
    }
}
