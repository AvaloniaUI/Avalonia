using System;
using System.Runtime.InteropServices;
using Avalonia.Platform.Interop;

// ReSharper disable InconsistentNaming
namespace Avalonia.Platform.Internal
{
    class UnixLoader : IDynamicLibraryLoader
    {
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
            if (unixName == "Darwin")
                OsXImports.Init();
            else
                LinuxImports.Init();
        }

        private static Func<string, int, IntPtr>? DlOpen;
        private static Func<IntPtr, string, IntPtr>? DlSym;
        private static Func<IntPtr>? DlError;
        // ReSharper restore InconsistentNaming

        static string? DlErrorString() => Marshal.PtrToStringAnsi(DlError!.Invoke());

        public IntPtr LoadLibrary(string dll)
        {
            var handle = DlOpen!.Invoke(dll, 1);
            if (handle == IntPtr.Zero)
                throw new DynamicLibraryLoaderException(DlErrorString()!);
            return handle;
        }

        public IntPtr GetProcAddress(IntPtr dll, string proc, bool optional)
        {
            var ptr = DlSym!.Invoke(dll, proc);
            if (ptr == IntPtr.Zero && !optional)
                throw new DynamicLibraryLoaderException(DlErrorString()!);
            return ptr;
        }
    }

    internal class Win32Loader : IDynamicLibraryLoader
    {
        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32", EntryPoint = "LoadLibraryW", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpszLib);

        IntPtr IDynamicLibraryLoader.LoadLibrary(string dll)
        {
            var handle = LoadLibrary(dll);
            if (handle != IntPtr.Zero)
                return handle;
            var err = Marshal.GetLastWin32Error();

            throw new DynamicLibraryLoaderException("Error loading " + dll + " error " + err);
        }

        IntPtr IDynamicLibraryLoader.GetProcAddress(IntPtr dll, string proc, bool optional)
        {
            var ptr = GetProcAddress(dll, proc);
            if (ptr == IntPtr.Zero && !optional)
                throw new DynamicLibraryLoaderException("Error " + Marshal.GetLastWin32Error());
            return ptr;
        }
    }

#if NET6_0_OR_GREATER
    internal class Net6Loader : IDynamicLibraryLoader
    {
        public IntPtr LoadLibrary(string dll)
        {
            try
            {
                return NativeLibrary.Load(dll);
            }
            catch (Exception ex)
            {
                throw new DynamicLibraryLoaderException("Error loading " + dll, ex);
            }
        }

        public IntPtr GetProcAddress(IntPtr dll, string proc, bool optional)
        {
            try
            {
                if (optional)
                {
                    return NativeLibrary.TryGetExport(dll, proc, out var address) ? address : default;
                }
                return NativeLibrary.GetExport(dll, proc);
            }
            catch (Exception ex)
            {
                throw new DynamicLibraryLoaderException("Error " + dll, ex);
            }
        }
    }
#endif
    
    internal class NotSupportedLoader : IDynamicLibraryLoader
    {
        IntPtr IDynamicLibraryLoader.LoadLibrary(string dll)
        {
            throw new PlatformNotSupportedException();
        }

        IntPtr IDynamicLibraryLoader.GetProcAddress(IntPtr dll, string proc, bool optional)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
