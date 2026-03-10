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
        public static IntPtr Load(string dll, Assembly assembly) => NativeLibrary.Load(dll, assembly, null);
        public static IntPtr Load(string dll) => NativeLibrary.Load(dll);
        public static bool TryGetExport(IntPtr handle, string name, out IntPtr address) =>
            NativeLibrary.TryGetExport(handle, name, out address);
    }
}
