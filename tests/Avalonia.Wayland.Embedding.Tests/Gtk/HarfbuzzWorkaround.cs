using System;
using System.IO;
using System.Runtime.InteropServices;

namespace XEmbedSample;

// Local copy of samples/XEmbedSample/HarfbuzzWorkaround.cs with explicit usings (the test project doesn't enable
// ImplicitUsings, so the shared file can't be Compile-Include'd). GtkSharp dlopen's system libharfbuzz with
// RTLD_GLOBAL, corrupting libHarfBuzzSharp's symbol resolution and segfaulting; reload it with RTLD_DEEPBIND.
public unsafe class HarfbuzzWorkaround
{
    [DllImport("libc")]
    static extern int dlinfo(IntPtr handle, int request, IntPtr info);

    [DllImport("libc")]
    static extern IntPtr dlopen(string filename, int flags);

    private const int RTLD_DI_ORIGIN = 6;
    private const int RTLD_NOW = 2;
    private const int RTLD_DEEPBIND = 8;

    public static void Apply()
    {
        if (RuntimeInformation.RuntimeIdentifier.Contains("musl"))
            throw new PlatformNotSupportedException("musl doesn't support RTLD_DEEPBIND");

        var libraryPathBytes = Marshal.AllocHGlobal(4096);
        var handle = NativeLibrary.Load("libHarfBuzzSharp", typeof(HarfBuzzSharp.Blob).Assembly, null);
        dlinfo(handle, RTLD_DI_ORIGIN, libraryPathBytes);
        var libraryOrigin = Marshal.PtrToStringUTF8(libraryPathBytes) ?? string.Empty;
        Marshal.FreeHGlobal(libraryPathBytes);
        var libraryPath = Path.Combine(libraryOrigin, "libHarfBuzzSharp.so");

        NativeLibrary.Free(handle);
        var forceLoadedHandle = dlopen(libraryPath, RTLD_NOW | RTLD_DEEPBIND);
        if (forceLoadedHandle == IntPtr.Zero)
            throw new DllNotFoundException($"Unable to load {libraryPath} via dlopen");

        NativeLibrary.SetDllImportResolver(typeof(HarfBuzzSharp.Blob).Assembly, (name, assembly, searchPath) =>
        {
            if (name.Contains("HarfBuzzSharp"))
                return dlopen(libraryPath, RTLD_NOW | RTLD_DEEPBIND);
            return NativeLibrary.Load(name, assembly, searchPath);
        });
    }
}
