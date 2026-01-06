using System;
using System.Runtime.InteropServices;

namespace Avalonia.IntegrationTests.Win32;

internal static partial class UnmanagedMethods
{
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetClientRect(IntPtr hwnd, out RECT lpRect);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }
}
