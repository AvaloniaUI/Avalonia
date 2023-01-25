using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace IntegrationTestApp
{
    public static class MacOSIntegration
    {
        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "sel_registerName")]
        private static extern IntPtr GetHandle(string name);
        
        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern long Int64_objc_msgSend(IntPtr receiver, IntPtr selector);

        private static readonly IntPtr s_orderedIndexSelector;

        static MacOSIntegration()
        {
            s_orderedIndexSelector = GetHandle("orderedIndex");;
        }
        
        public static long GetOrderedIndex(Window window)
        {
            return Int64_objc_msgSend(window.PlatformImpl!.Handle.Handle, s_orderedIndexSelector);
        }
    }
}
