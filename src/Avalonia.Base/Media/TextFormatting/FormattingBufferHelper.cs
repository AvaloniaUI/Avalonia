using System.Collections.Generic;
using System.Numerics;
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

            if (IsBufferTooLarge<T>((uint) arrayBuilder.Capacity))
            {
                arrayBuilder = default;
            }
        }

        public static void ClearThenResetIfTooLarge<T>(List<T> list)
        {
            list.Clear();

            if (IsBufferTooLarge<T>((uint) list.Capacity))
            {
                list.TrimExcess();
            }
        }

        public static void ClearThenResetIfTooLarge<T>(Stack<T> stack)
        {
            var approximateCapacity = RoundUpToPowerOf2((uint)stack.Count);

            stack.Clear();

            if (IsBufferTooLarge<T>(approximateCapacity))
            {
                stack.TrimExcess();
            }
        }

        public static void ClearThenResetIfTooLarge<TKey, TValue>(ref Dictionary<TKey, TValue> dictionary)
            where TKey : notnull
        {
            var approximateCapacity = RoundUpToPowerOf2((uint)dictionary.Count);

            dictionary.Clear();

            // dictionary is in fact larger than that: it has entries and buckets, but let's only count our data here
            if (IsBufferTooLarge<KeyValuePair<TKey, TValue>>(approximateCapacity))
            {
                dictionary.TrimExcess();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsBufferTooLarge<T>(uint capacity)
            => (long) (uint) Unsafe.SizeOf<T>() * capacity > MaxKeptBufferSizeInBytes;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint RoundUpToPowerOf2(uint value)
        {
            return BitOperations.RoundUpToPowerOf2(value);
        }
    }
}
