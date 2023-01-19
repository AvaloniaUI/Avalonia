using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    internal static class FormattingBufferHelper
    {
        // 1MB, arbitrary, that's 512K characters or 128K object references on x64
        private const long MaxKeptBufferSizeInBytes = 1024 * 1024;

        public static void ClearThenResetIfTooLarge<T>(ref ArrayBuilder<T> arrayBuilder)
        {
            arrayBuilder.Clear();

            if (IsBufferTooLarge<T>(arrayBuilder.Capacity))
            {
                arrayBuilder = default;
            }
        }

        public static void ClearThenResetIfTooLarge<T>(List<T> list)
        {
            list.Clear();

            if (IsBufferTooLarge<T>(list.Capacity))
            {
                list.TrimExcess();
            }
        }

        public static void ClearThenResetIfTooLarge<T>(Stack<T> stack)
        {
            stack.Clear();

            if (IsBufferTooLarge<T>(stack.Count))
            {
                stack.TrimExcess();
            }
        }

        public static void ClearThenResetIfTooLarge<TKey, TValue>(ref Dictionary<TKey, TValue> dictionary)
            where TKey : notnull
        {
            dictionary.Clear();

            // dictionary is in fact larger than that: it has entries and buckets, but let's only count our data here
            if (IsBufferTooLarge<KeyValuePair<TKey, TValue>>(dictionary.Count))
            {
#if NET6_0_OR_GREATER
                dictionary.TrimExcess();
#else
                dictionary = new Dictionary<TKey, TValue>();
#endif
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsBufferTooLarge<T>(int length)
            => (long)Unsafe.SizeOf<T>() * length > MaxKeptBufferSizeInBytes;
    }
}
