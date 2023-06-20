using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Avalonia.FreeDesktop
{
    internal static class NativeMethods
    {
        [DllImport("libc", SetLastError = true)]
        private static extern long readlink([MarshalAs(UnmanagedType.LPArray)] byte[] filename,
                                            [MarshalAs(UnmanagedType.LPArray)] byte[] buffer,
                                            long len);

        public static string ReadLink(string path)
        {
            var symlinkSize = Encoding.UTF8.GetByteCount(path);
            const int BufferSize = 4097; // PATH_MAX is (usually?) 4096, but we need to know if the result was truncated

            var symlink = ArrayPool<byte>.Shared.Rent(symlinkSize + 1);
            var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);

            try
            {
                Encoding.UTF8.GetBytes(path, 0, path.Length, symlink, 0);
                symlink[symlinkSize] = 0;

                var size = readlink(symlink, buffer, BufferSize);
                Debug.Assert(size < BufferSize); // if this fails, we need to increase the buffer size (dynamically?)

                return Encoding.UTF8.GetString(buffer, 0, (int)size);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(symlink);
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
