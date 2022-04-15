using System;

namespace Avalonia
{
    internal class EnumParserHelper
    {
#if NET6_0
        public static T ParseEnum<T>(ReadOnlySpan<char> key, bool ignoreCase) where T : struct
        {
            return Enum.Parse<T>(key, ignoreCase);
        }
#else
        public static T ParseEnum<T>(string key, bool ignoreCase) where T : struct
        {
            return (T)Enum.Parse(typeof(T), key, ignoreCase);
        }
#endif
    }
}
