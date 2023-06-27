using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia.Compatibility;
using Avalonia.Platform.Interop;

namespace Avalonia.Compatibility
{
    internal class NativeLibraryEx
    {
#if NET6_0_OR_GREATER
        public static IntPtr Load(string dll, Assembly assembly) => NativeLibrary.Load(dll, assembly, null);
        public static IntPtr Load(string dll) => NativeLibrary.Load(dll);
        public static bool TryGetExport(IntPtr handle, string name, out IntPtr address) =>
            NativeLibrary.TryGetExport(handle, name, out address);
#else
        public static IntPtr Load(string dll, Assembly assembly) => Load(dll);
        public static IntPtr Load(string dll)
        {
            var handle = DlOpen!(dll);
            if (handle != IntPtr.Zero)
                return handle;
            throw new InvalidOperationException("Unable to load " + dll, DlError!());
        }

        public static bool TryGetExport(IntPtr handle, string name, out IntPtr address)
        {
            try
            {
                address = DlSym!(handle, name);
                return address != default;
            }
            catch (Exception)
            {
                address = default;
                return false;
            }
        }
        
        static NativeLibraryEx()
        {
            if (OperatingSystemEx.IsWindows())
            {
                Win32Imports.Init();
            }
            else if (OperatingSystemEx.IsLinux() || OperatingSystemEx.IsMacOS())
            {
                var buffer = Marshal.AllocHGlobal(0x1000);
                uname(buffer);
                var unixName = Marshal.PtrToStringAnsi(buffer);
                Marshal.FreeHGlobal(buffer);
                if (unixName == "Darwin")
                    OsXImports.Init();
                else
                    LinuxImports.Init();
            }
        }

        private static Func<string, IntPtr>? DlOpen;
        private static Func<IntPtr, string, IntPtr>? DlSym;
        private static Func<Exception?>? DlError;

        [DllImport("libc")]
        static extern int uname(IntPtr buf);

        static class Win32Imports
        {
            [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
            private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

            [DllImport("kernel32", EntryPoint = "LoadLibraryW", SetLastError = true, CharSet = CharSet.Unicode)]
            private static extern IntPtr LoadLibrary(string lpszLib);

            public static void Init()
            {
                DlOpen = LoadLibrary;
                DlSym = GetProcAddress;
                DlError = () => new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
        
        static class LinuxImports
        {
            [DllImport("libdl.so.2")]
            private static extern IntPtr dlopen(string path, int flags);

            [DllImport("libdl.so.2")]
            private static extern IntPtr dlsym(IntPtr handle, string symbol);

            [DllImport("libdl.so.2")]
            private static extern IntPtr dlerror();

            public static void Init()
            {
                DlOpen = s => dlopen(s, 1);
                DlSym = dlsym;
                DlError = () => new InvalidOperationException(Marshal.PtrToStringAnsi(dlerror()));
            }
        }

        static class OsXImports
        {
            [DllImport("/usr/lib/libSystem.dylib")]
            private static extern IntPtr dlopen(string path, int flags);

            [DllImport("/usr/lib/libSystem.dylib")]
            private static extern IntPtr dlsym(IntPtr handle, string symbol);

            [DllImport("/usr/lib/libSystem.dylib")]
            private static extern IntPtr dlerror();

            public static void Init()
            {
                DlOpen = s => dlopen(s, 1);
                DlSym = dlsym;
                DlError = () => new InvalidOperationException(Marshal.PtrToStringAnsi(dlerror()));
            }
        }
#endif
    }
}
