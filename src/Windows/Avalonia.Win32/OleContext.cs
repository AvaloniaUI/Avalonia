using System;
using System.ComponentModel;
using System.Threading;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32
{
    class OleContext
    {
        private static OleContext fCurrent;

        internal static OleContext Current
        {
            get
            {
                if (!IsValidOleThread())
                    return null;

                if (fCurrent == null)
                    fCurrent = new OleContext();
                return fCurrent;
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

        internal bool RegisterDragDrop(IPlatformHandle hwnd, IDropTarget target)
        {
            if (hwnd?.HandleDescriptor != "HWND" || target == null)
                return false;

            return UnmanagedMethods.RegisterDragDrop(hwnd.Handle, target) == UnmanagedMethods.HRESULT.S_OK;
        }
    }
}
