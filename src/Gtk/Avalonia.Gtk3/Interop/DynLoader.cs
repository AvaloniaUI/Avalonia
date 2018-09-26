using System;
using System.Runtime.InteropServices;

/*
 * Source code imported from https://github.com/kekekeks/evhttp-sharp
 * Source is provided under MIT license for Avalonia project and derived works
 */


namespace Avalonia.Gtk3.Interop
{
    internal interface IDynLoader
    {
        IntPtr LoadLibrary(string dll);
        IntPtr GetProcAddress(IntPtr dll, string proc, bool optional);

    }

    class UnixLoader : IDynLoader
    {
        // ReSharper disable InconsistentNaming
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
                DlOpen = dlopen;
                DlSym = dlsym;
                DlError = dlerror;
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
                DlOpen = dlopen;
                DlSym = dlsym;
                DlError = dlerror;
            }
            
        }
        

        [DllImport("libc")]
        static extern int uname(IntPtr buf);

        static UnixLoader()
        {
            var buffer = Marshal.AllocHGlobal(0x1000);
            uname(buffer);
            var unixName = Marshal.PtrToStringAnsi(buffer);
            Marshal.FreeHGlobal(buffer);
            if(unixName == "Darwin")
                OsXImports.Init();
            else
                LinuxImports.Init();
        }

        private static Func<string, int, IntPtr> DlOpen;
        private static Func<IntPtr, string, IntPtr> DlSym;
        private static Func<IntPtr> DlError;
        // ReSharper restore InconsistentNaming

        static string DlErrorString() => Marshal.PtrToStringAnsi(DlError());

        public IntPtr LoadLibrary(string dll)
        {
            var handle = DlOpen(dll, 1);
            if (handle == IntPtr.Zero)
                throw new NativeException(DlErrorString());
            return handle;
        }

        public IntPtr GetProcAddress(IntPtr dll, string proc, bool optional)
        {
            var ptr = DlSym(dll, proc);
            if (ptr == IntPtr.Zero && !optional)
                throw new NativeException(DlErrorString());
            return ptr;
        }
    }

    internal class Win32Loader : IDynLoader
    {
        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32", EntryPoint = "LoadLibraryW", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpszLib);

        IntPtr IDynLoader.LoadLibrary(string dll)
        {
            var handle = LoadLibrary(dll);
            if (handle != IntPtr.Zero)
                return handle;
            var err = Marshal.GetLastWin32Error();

            throw new NativeException("Error loading " + dll + " error " + err);
        }

        IntPtr IDynLoader.GetProcAddress(IntPtr dll, string proc, bool optional)
        {
            var ptr = GetProcAddress(dll, proc);
            if (ptr == IntPtr.Zero && !optional)
                throw new NativeException("Error " + Marshal.GetLastWin32Error());
            return ptr;
        }
    }
}
