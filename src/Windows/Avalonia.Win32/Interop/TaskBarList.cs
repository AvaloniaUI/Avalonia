using System;
using System.Runtime.InteropServices;
using System.Threading;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32.Interop
{
    internal class TaskBarList
    {
        private static IntPtr s_taskBarList;
        private static bool s_initialized;
        private static object s_lock = new();

        private static HrInit? s_hrInitDelegate;
        private static MarkFullscreenWindow? s_markFullscreenWindowDelegate;
        private static SetOverlayIcon? s_setOverlayIconDelegate;

        private static unsafe IntPtr Init()
        {
            int result = CoCreateInstance(in ShellIds.TaskBarList, IntPtr.Zero, 1, in ShellIds.ITaskBarList2, out IntPtr instance);

            var ptr = (ITaskBarList3VTable**)instance.ToPointer();

            s_hrInitDelegate ??= Marshal.GetDelegateForFunctionPointer<HrInit>((*ptr)->HrInit);

            if (s_hrInitDelegate(instance) != HRESULT.S_OK)
            {
                return IntPtr.Zero;
            }

            return instance;
        }

        private static IntPtr LazyInit() => LazyInitializer.EnsureInitialized(ref s_taskBarList, ref s_initialized, ref s_lock, Init);

        /// <summary>
        /// Ported from https://github.com/chromium/chromium/blob/master/ui/views/win/fullscreen_handler.cc
        /// </summary>
        /// <param name="hwnd">The window handle.</param>
        /// <param name="fullscreen">Fullscreen state.</param>
        public static unsafe void MarkFullscreen(IntPtr hwnd, bool fullscreen)
        {
            LazyInit();

            if (s_taskBarList != IntPtr.Zero)
            {
                var ptr = (ITaskBarList3VTable**)s_taskBarList.ToPointer();

                s_markFullscreenWindowDelegate ??=
                    Marshal.GetDelegateForFunctionPointer<MarkFullscreenWindow>((*ptr)->MarkFullscreenWindow);

                s_markFullscreenWindowDelegate(s_taskBarList, hwnd, fullscreen);
            }
        }

        public static unsafe void SetOverlayIcon(IntPtr hwnd, IntPtr hIcon, string? description)
        {
            LazyInit();

            if (s_taskBarList != IntPtr.Zero)
            {
                var ptr = (ITaskBarList3VTable**)s_taskBarList.ToPointer();

                s_setOverlayIconDelegate ??=
                    Marshal.GetDelegateForFunctionPointer<SetOverlayIcon>((*ptr)->SetOverlayIcon);

                s_setOverlayIconDelegate(s_taskBarList, hwnd, hIcon, description);
            }
        }
    }
}
