using System;
using System.Runtime.CompilerServices;

namespace Avalonia
{
    /// <summary>
    /// Provides extension methods for enums.
    /// </summary>
    public static class EnumExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool HasFlagCustom<T>(this T value, T flag) where T : unmanaged, Enum
        {
            if (sizeof(T) == 1)
            {
                var byteValue = Unsafe.As<T, byte>(ref value);
                var byteFlag = Unsafe.As<T, byte>(ref flag);
                return (byteValue & byteFlag) == byteFlag;
            }
            else if (sizeof(T) == 2)
            {
                var shortValue = Unsafe.As<T, short>(ref value);
                var shortFlag = Unsafe.As<T, short>(ref flag);
                return (shortValue & shortFlag) == shortFlag;
            }
            else if (sizeof(T) == 4)
            {
                var intValue = Unsafe.As<T, int>(ref value);
                var intFlag = Unsafe.As<T, int>(ref flag);
                return (intValue & intFlag) == intFlag;
            }
            else if (sizeof(T) == 8)
            {
                var longValue = Unsafe.As<T, long>(ref value);
                var longFlag = Unsafe.As<T, long>(ref flag);
                return (longValue & longFlag) == longFlag;
            }
            else
                throw new NotSupportedException("Enum with size of " + Unsafe.SizeOf<T>() + " are not supported");
        }
    }
}
