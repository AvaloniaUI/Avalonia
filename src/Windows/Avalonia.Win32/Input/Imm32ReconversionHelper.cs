using System;
using System.Runtime.InteropServices;

using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32.Input
{
    /// <summary>
    /// Reads and writes the <see cref="RECONVERTSTRING"/> structure exchanged with an IME during
    /// reconversion (WM_IME_REQUEST / IMR_RECONVERTSTRING / IMR_CONFIRMRECONVERTSTRING). The logic is
    /// kept free of any window or client dependency so it can be unit tested against a raw buffer.
    /// </summary>
    internal static class Imm32ReconversionHelper
    {
        private static readonly int s_headerSize = Marshal.SizeOf<RECONVERTSTRING>();

        /// <summary>
        /// The number of bytes a <see cref="RECONVERTSTRING"/> needs to hold a string of the given length.
        /// </summary>
        public static int GetRequiredSize(int textLength)
        {
            if (textLength < 0)
            {
                textLength = 0;
            }

            return s_headerSize + textLength * sizeof(char);
        }

        /// <summary>
        /// Fills the <see cref="RECONVERTSTRING"/> at <paramref name="buffer"/> with <paramref name="text"/>
        /// and a composition/target range described by <paramref name="compStart"/> and
        /// <paramref name="compLen"/> (char offsets within <paramref name="text"/>).
        /// </summary>
        /// <returns>
        /// The total number of bytes required for the structure. When <paramref name="buffer"/> is null or
        /// too small to hold the data nothing is written, so the IME can use the return value to size or
        /// resize its buffer.
        /// </returns>
        public static unsafe int Write(IntPtr buffer, ReadOnlySpan<char> text, int compStart, int compLen)
        {
            var needed = GetRequiredSize(text.Length);

            if (buffer == IntPtr.Zero)
            {
                return needed;
            }

            var reconv = (RECONVERTSTRING*)buffer;

            // On the fill pass the IME allocates the buffer and reports its capacity in dwSize. Don't write
            // past it; returning the needed size lets the IME reallocate and ask again.
            if (reconv->dwSize < (uint)needed)
            {
                return needed;
            }

            compStart = Math.Clamp(compStart, 0, text.Length);
            compLen = Math.Clamp(compLen, 0, text.Length - compStart);

            reconv->dwVersion = 0;
            reconv->dwStrLen = (uint)text.Length;
            reconv->dwStrOffset = (uint)s_headerSize;
            reconv->dwCompStrLen = (uint)compLen;
            reconv->dwCompStrOffset = (uint)(compStart * sizeof(char));
            reconv->dwTargetStrLen = (uint)compLen;
            reconv->dwTargetStrOffset = (uint)(compStart * sizeof(char));

            var destination = new Span<char>((byte*)buffer + s_headerSize, text.Length);
            text.CopyTo(destination);

            return needed;
        }

        /// <summary>
        /// Reads the composition range the IME settled on, as char offsets within the provided string.
        /// </summary>
        /// <returns><c>false</c> when the buffer is null or the range is out of bounds.</returns>
        public static unsafe bool ReadCompRange(IntPtr buffer, out int compStart, out int compLen)
        {
            compStart = 0;
            compLen = 0;

            if (buffer == IntPtr.Zero)
            {
                return false;
            }

            var reconv = (RECONVERTSTRING*)buffer;

            var strLen = reconv->dwStrLen;
            var start = reconv->dwCompStrOffset / (uint)sizeof(char);
            var length = reconv->dwCompStrLen;

            // Guard against a malformed structure before narrowing to int.
            if (start > strLen || length > strLen - start)
            {
                return false;
            }

            compStart = (int)start;
            compLen = (int)length;

            return true;
        }
    }
}
