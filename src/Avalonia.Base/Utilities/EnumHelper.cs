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

        public static bool TryParse<T>(ReadOnlySpan<char> key, bool ignoreCase, out T result) where T : struct
        {
            return Enum.TryParse(key, ignoreCase, out result);
        }
#else
        public static T Parse<T>(string key, bool ignoreCase) where T : struct
        {
            return (T)Enum.Parse(typeof(T), key, ignoreCase);
        }

        public static bool TryParse<T>(string key, bool ignoreCase, out T result) where T : struct
        {
            return Enum.TryParse(key, ignoreCase, out result);
        }

        public static bool TryParse<T>(ReadOnlySpan<char> key, bool ignoreCase, out T result) where T : struct
        {
            return Enum.TryParse(key.ToString(), ignoreCase, out result);
        }
#endif
    }
}
