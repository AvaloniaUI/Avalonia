using System;

namespace Avalonia.Utilities
{
    internal class EnumHelper
    {
#if NET6_0_OR_GREATER
        public static T Parse<T>(ReadOnlySpan<char> key, bool ignoreCase) where T : struct
        {
            return Enum.Parse<T>(key, ignoreCase);
        }
#else
        public static T Parse<T>(string key, bool ignoreCase) where T : struct
        {
            return (T)Enum.Parse(typeof(T), key, ignoreCase);
        }
#endif
    }
}
