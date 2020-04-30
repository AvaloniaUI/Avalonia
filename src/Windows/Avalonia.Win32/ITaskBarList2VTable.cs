using System;
using System.Runtime.InteropServices;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32
{
    delegate void MarkFullscreenWindow(IntPtr This, IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fullscreen);
    delegate HRESULT HrInit(IntPtr This);

    struct ITaskBarList2VTable
    {
        public IntPtr IUnknown1;
        public IntPtr IUnknown2;
        public IntPtr IUnknown3;
        public IntPtr HrInit;
        public IntPtr AddTab;
        public IntPtr DeleteTab;
        public IntPtr ActivateTab;
        public IntPtr SetActiveAlt;
        public IntPtr MarkFullscreenWindow;
    }
}
