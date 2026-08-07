using System.Runtime.InteropServices;

namespace WaylandEmbedSample;

internal static partial class LibcInterop
{
    // libwayland's wl_display_connect(NULL) reads WAYLAND_SOCKET from libc's `environ`. We MUST set it via
    // libc setenv, NOT Environment.SetEnvironmentVariable, which only updates the CLR's cached copy. (research/03)
    [LibraryImport("libc", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int setenv(string name, string value, int overwrite);

    public static void SetEnv(string name, string value) => setenv(name, value, 1);
}
