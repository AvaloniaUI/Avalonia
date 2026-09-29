using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Avalonia.Harfbuzz
{
    /// <summary>
    /// HarfBuzz entry points that HarfBuzzSharp exports natively but does not wrap.
    /// </summary>
    internal static unsafe partial class HarfBuzzNative
    {
        // The library name HarfBuzzSharp uses in its own imports, so both resolve to the one
        // loaded binary (and to the same statically linked archive on WebAssembly).
        private const string LibraryName = "libHarfBuzzSharp";

        // HarfBuzzSharp's iOS, tvOS and Mac Catalyst builds import from the embedded
        // framework; this assembly has no per-platform builds, so it maps the name at runtime.
        private const string AppleFrameworkPath = "@rpath/libHarfBuzzSharp.framework/libHarfBuzzSharp";

        private static bool s_isUnavailable;

        static HarfBuzzNative()
        {
            if (OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() || OperatingSystem.IsMacCatalyst())
            {
                NativeLibrary.SetDllImportResolver(typeof(HarfBuzzNative).Assembly, ResolveAppleFramework);
            }
        }

        /// <summary>
        /// Sets normalized variation coordinates, in fvar axis order and F2Dot14 units, on
        /// <paramref name="font"/>.
        /// </summary>
        /// <returns>
        /// <c>false</c> when the loaded native library lacks the entry point; the font is left
        /// at the default instance.
        /// </returns>
        public static bool TrySetVarCoordsNormalized(IntPtr font, ReadOnlySpan<int> coords)
        {
            if (s_isUnavailable)
            {
                return false;
            }

            try
            {
                fixed (int* pCoords = coords)
                {
                    hb_font_set_var_coords_normalized(font, pCoords, (uint)coords.Length);
                }

                return true;
            }
            catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
            {
                s_isUnavailable = true;
                return false;
            }
        }

        private static IntPtr ResolveAppleFramework(
            string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (libraryName == LibraryName && NativeLibrary.TryLoad(AppleFrameworkPath, out var handle))
            {
                return handle;
            }

            return IntPtr.Zero;
        }

        [LibraryImport(LibraryName)]
        private static partial void hb_font_set_var_coords_normalized(IntPtr font, int* coords, uint coordsLength);
    }
}
