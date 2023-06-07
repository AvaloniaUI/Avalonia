using System;

namespace Avalonia.Utilities
{
    internal static class ByteSizeHelper
    {
        private const string formatTemplateSeparated = "{0}{1:0.#} {2}";
        private const string formatTemplate = "{0}{1:0.#}{2}";

        private static readonly string[] Prefixes =
        {
            "B",
            "KB",
            "MB",
            "GB",
            "TB",
            "PB",
            "EB",
            "ZB",
            "YB" 
        };

        public static string ToString(ulong bytes, bool separate)
        {
            if (bytes == 0)
            {
                return string.Format(separate ? formatTemplateSeparated : formatTemplate, null, 0, Prefixes[0]);
            }

            var absSize = Math.Abs((double)bytes);
            var fpPower = Math.Log(absSize, 1000);
            var intPower = (int)fpPower;
            var iUnit = intPower >= Prefixes.Length
                ? Prefixes.Length - 1
                : intPower;
            var normSize = absSize / Math.Pow(1000, iUnit);

            return string.Format(formatTemplate,bytes < 0 ? "-" : null, normSize, Prefixes[iUnit]);
        }
    }
}
