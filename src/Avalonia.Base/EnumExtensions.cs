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
            var intValue = *(int*)&value;
            var intFlag = *(int*)&flag;

            return (intValue & intFlag) == intFlag;
        }
    }
}
