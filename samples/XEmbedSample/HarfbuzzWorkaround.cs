using System.Runtime.InteropServices;

namespace XEmbedSample;

/*
 This is needed specifically for GtkSharp:
 https://github.com/mono/SkiaSharp/issues/3038
 https://github.com/GtkSharp/GtkSharp/issues/443
 
 Instead of using plain DllImport they are manually calling dlopen with RTLD_GLOBAL and RTLD_LAZY flags:
 https://github.com/GtkSharp/GtkSharp/blob/b7303616129ab5a0ca64def45649ab522d83fa4a/Source/Libs/Shared/FuncLoader.cs#L80-L92
 
 Which causes libHarfBuzzSharp.so from HarfBuzzSharp to resolve some of the symbols from the system libharfbuzz.so.0
 which is a _different_ harfbuzz version.
 
 That results in a segfault.
 
 Previously there was a workaround - https://github.com/mono/SkiaSharp/pull/2247 but it got 
 disabled for .NET Core / .NET 5+.
 
 Why linux linker builds shared libraries in a way that makes it possible for them to resolve their own symbols from
 elsewhere escapes me.
 
 Here we are loading libHarfBuzzSharp.so from the .NET-resolved location, saving it, unloading the library
 and then defining a custom resolver that would call dlopen with RTLD_NOW + RTLD_DEEPBIND 

 */
 
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
        var libraryOrigin = Marshal.PtrToStringUTF8(libraryPathBytes);
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