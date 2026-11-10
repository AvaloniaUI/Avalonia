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

    [LibraryImport("user32.dll")]
    public static partial int GetSystemMetrics(int nIndex);

    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    public const int SM_CMONITORS = 80;

    [LibraryImport("user32.dll")]
    public static partial void keybd_event(byte bVk, byte bScan, uint dwFlags, nuint dwExtraInfo);

    [LibraryImport("user32.dll")]
    public static partial IntPtr GetForegroundWindow();

    [LibraryImport("user32.dll", EntryPoint = "LoadKeyboardLayoutW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial IntPtr LoadKeyboardLayout(string pwszKLID, uint flags);

    [LibraryImport("user32.dll")]
    public static partial IntPtr ActivateKeyboardLayout(IntPtr hkl, uint flags);

    [LibraryImport("user32.dll")]
    public static partial IntPtr GetKeyboardLayout(uint idThread);

    [LibraryImport("user32.dll")]
    public static partial int GetKeyboardLayoutList(int nBuff, IntPtr[]? lpList);

    public const uint KEYEVENTF_KEYUP = 0x2;
    public const uint KLF_ACTIVATE = 0x1;
    public const uint KLF_NOTELLSHELL = 0x80;
}
