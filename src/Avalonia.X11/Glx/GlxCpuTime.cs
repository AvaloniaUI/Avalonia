using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Avalonia.X11.Glx
{
    /// <summary>
    /// Process-wide CPU time (user + system) on Linux, used to tell a stuck GL session
    /// (driver spinning in CPU on its own thread) from a legitimately slow GPU
    /// (which only sleeps the render thread on a fence).
    /// </summary>
    internal static class GlxCpuTime
    {
        private const int RUSAGE_SELF = 0;

        // struct rusage is 144 bytes on x86_64; only the first two timevals are used.
        private static readonly ThreadLocal<byte[]> Buffer = new(() => new byte[256]);

        [DllImport("c")]
        private static extern int getrusage(int who, byte[] usage);

        public static long CpuTimeMicroseconds()
        {
            var buffer = Buffer.Value!;
            if (getrusage(RUSAGE_SELF, buffer) != 0)
                return 0;
            var utime = BitConverter.ToInt64(buffer, 0) * 1_000_000L + BitConverter.ToInt32(buffer, 8);
            var stime = BitConverter.ToInt64(buffer, 16) * 1_000_000L + BitConverter.ToInt32(buffer, 24);
            return utime + stime;
        }
    }
}
