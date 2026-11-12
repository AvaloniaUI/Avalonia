using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace RendererBench.Timing;

/// <summary>
/// Reads process and GPU memory. Cheap enough to call once per frame.
/// </summary>
internal static unsafe class MemoryProbe
{
    private const int TaskVmInfo = 22;
    private const int PhysFootprintOffset = 144;
    private const double Mb = 1024.0 * 1024.0;

    private static IntPtr s_metalDevice;

    /// <summary>
    /// Physical footprint on macOS (what Activity Monitor shows) or resident set on Linux, in MB. Covers native
    /// allocations, managed heap and GPU memory that the OS counts against the process.
    /// </summary>
    public static double FootprintMb()
    {
        if (!OperatingSystem.IsMacOS())
            return ReadProcStatusMb("VmRSS:");

        var info = stackalloc byte[512];
        var count = 512 / sizeof(int);
        return task_info(TaskSelf(), TaskVmInfo, info, &count) == 0
            ? *(ulong*)(info + PhysFootprintOffset) / Mb
            : double.NaN;
    }

    /// <summary>Peak resident set of the process in MB.</summary>
    public static double PeakRssMb()
    {
        if (!OperatingSystem.IsMacOS())
            return ReadProcStatusMb("VmHWM:");

        // ru_maxrss follows two timevals in struct rusage and is in bytes on macOS.
        var usage = stackalloc long[18];
        return getrusage(0, usage) == 0 ? usage[4] / Mb : double.NaN;
    }

    public static void SetMetalDevice(IntPtr device) => s_metalDevice = device;

    /// <summary>GPU memory the process allocated on the Metal device in MB, or null without Metal.</summary>
    public static double? GpuMemoryMb() =>
        s_metalDevice == IntPtr.Zero
            ? null
            : (ulong)objc_msgSend(s_metalDevice, sel_registerName("currentAllocatedSize")) / Mb;

    private static double ReadProcStatusMb(string key)
    {
        try
        {
            var line = File.ReadLines("/proc/self/status").FirstOrDefault(l => l.StartsWith(key));
            if (line is null)
                return double.NaN;
            var kb = double.Parse(line[key.Length..].Trim().Split(' ')[0], CultureInfo.InvariantCulture);
            return kb / 1024;
        }
        catch (IOException)
        {
            return double.NaN;
        }
    }

    // mach_task_self() is a macro that reads this global.
    private static uint TaskSelf() =>
        *(uint*)NativeLibrary.GetExport(NativeLibrary.Load("libc"), "mach_task_self_");

    [DllImport("libc")]
    private static extern int task_info(uint task, int flavor, byte* info, int* count);

    [DllImport("libc")]
    private static extern int getrusage(int who, long* usage);

    [DllImport("/usr/lib/libobjc.dylib")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.dylib")]
    private static extern IntPtr sel_registerName(string name);
}
