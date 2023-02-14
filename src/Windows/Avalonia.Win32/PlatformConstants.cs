using System;

namespace Avalonia.Win32
{
    public static class PlatformConstants
    {
        public const string WindowHandleType = "HWND";
        public const string CursorHandleType = "HCURSOR";

        internal static readonly Version Windows8 = new Version(6, 2);
        internal static readonly Version Windows7 = new Version(6, 1);
    }
}
