using System;
using System.Collections.Generic;
using System.Text;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;

namespace Avalonia.MonoMac
{
    public static class ThreadingUtils
    {
        private static readonly IntPtr selUnlockFocusHandle = Selector.GetHandle("unlockFocus");
        private static readonly IntPtr selLockFocusIfCanDrawHandle = Selector.GetHandle("lockFocusIfCanDraw");
        private static readonly IntPtr selWindowHandle = Selector.GetHandle("window");
        private static readonly IntPtr selCanDrawHandle = Selector.GetHandle("canDraw");

        public static bool NonUILockFocusIfCanDraw(this NSView view)
        {
            return Messaging.bool_objc_msgSend(view.Handle, selLockFocusIfCanDrawHandle);
        }

        public static void NonUIUnlockFocus(this NSView view)
        {
            Messaging.void_objc_msgSend(view.Handle, selUnlockFocusHandle);
        }

        public static NSWindow NonUIGetWindow(this NSView view)
        {
            return (NSWindow) Runtime.GetNSObject(Messaging.IntPtr_objc_msgSendSuper(view.Handle, selWindowHandle));
        }

        public static bool BaseCanDraw(this NSView view)
        {
            return Messaging.bool_objc_msgSendSuper(view.SuperHandle, selCanDrawHandle);
        }
    }
}
