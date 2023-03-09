using System;
using System.ComponentModel;
using System.Threading;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.Win32.Interop;
using Avalonia.Win32.Win32Com;
using MicroCom.Runtime;

namespace Avalonia.Win32
{
    internal class OleContext
    {
        private static OleContext? s_current;

        internal static OleContext? Current
        {
            get
            {
                if (!IsValidOleThread())
                    return null;

                return s_current ??= new OleContext();
            }
        }

        private OleContext()
        {
            UnmanagedMethods.HRESULT res = UnmanagedMethods.OleInitialize(IntPtr.Zero);

            if (res != UnmanagedMethods.HRESULT.S_OK &&
                res != UnmanagedMethods.HRESULT.S_FALSE /*already initialized*/)
                throw new Win32Exception((int)res, "Failed to initialize OLE");
        }

        private static bool IsValidOleThread()
        {
            return Dispatcher.UIThread.CheckAccess() &&
                   Thread.CurrentThread.GetApartmentState() == ApartmentState.STA;
        }

        internal bool RegisterDragDrop(IPlatformHandle? hwnd, IDropTarget? target)
        {
            if (hwnd?.HandleDescriptor != "HWND" || target == null)
            {
                return false;
            }

            var trgPtr = target.GetNativeIntPtr();
            return UnmanagedMethods.RegisterDragDrop(hwnd.Handle, trgPtr) == UnmanagedMethods.HRESULT.S_OK;
        }

        internal bool UnregisterDragDrop(IPlatformHandle? hwnd)
        {
            if (hwnd?.HandleDescriptor != "HWND")
            {
                return false;
            }

            return UnmanagedMethods.RevokeDragDrop(hwnd.Handle) == UnmanagedMethods.HRESULT.S_OK;
        }
    }
}
