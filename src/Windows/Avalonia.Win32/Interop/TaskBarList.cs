using System;
using System.Runtime.InteropServices;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32.Interop
{
    internal class TaskBarList
    {
        private static IntPtr s_taskBarList;
        private static HrInit? s_hrInitDelegate;
        private static MarkFullscreenWindow? s_markFullscreenWindowDelegate;

        /// <summary>
        /// Ported from https://github.com/chromium/chromium/blob/master/ui/views/win/fullscreen_handler.cc
        /// </summary>
        /// <param name="hwnd">The window handle.</param>
        /// <param name="fullscreen">Fullscreen state.</param>
        public static unsafe void MarkFullscreen(IntPtr hwnd, bool fullscreen)
        {
            if (s_taskBarList == IntPtr.Zero)
            {
                Guid clsid = ShellIds.TaskBarList;
                Guid iid = ShellIds.ITaskBarList2;

                int result = CoCreateInstance(ref clsid, IntPtr.Zero, 1, ref iid, out s_taskBarList);

                if (s_taskBarList != IntPtr.Zero)
                {
                    var ptr = (ITaskBarList2VTable**)s_taskBarList.ToPointer();

                    s_hrInitDelegate ??= Marshal.GetDelegateForFunctionPointer<HrInit>((*ptr)->HrInit);

                    if (s_hrInitDelegate(s_taskBarList) != HRESULT.S_OK)
                    {
                        s_taskBarList = IntPtr.Zero;
                    }
                }
            }

            if (s_taskBarList != IntPtr.Zero)
            {
                var ptr = (ITaskBarList2VTable**)s_taskBarList.ToPointer();

                s_markFullscreenWindowDelegate ??=
                    Marshal.GetDelegateForFunctionPointer<MarkFullscreenWindow>((*ptr)->MarkFullscreenWindow);

                s_markFullscreenWindowDelegate(s_taskBarList, hwnd, fullscreen);
            }
        }
    }
}
