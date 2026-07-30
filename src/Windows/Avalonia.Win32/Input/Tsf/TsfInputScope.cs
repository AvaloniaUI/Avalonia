using System;
using System.Runtime.InteropServices;
using Avalonia.Input.TextInput;

namespace Avalonia.Win32.Input.Tsf
{
    /// <summary>
    /// Publishes the focused editable's content type as the window's input scope, which
    /// text services read to pick conversion modes and keyboard layouts (the same signal
    /// the touch keyboard and handwriting panel use). Values are the inputscope.h
    /// constants.
    /// </summary>
    internal static class TsfInputScope
    {
        private const int IS_DEFAULT = 0;
        private const int IS_URL = 1;
        private const int IS_EMAIL_SMTPEMAILADDRESS = 5;
        private const int IS_PERSONALNAME_FULLNAME = 7;
        private const int IS_DIGITS = 28;
        private const int IS_NUMBER = 29;
        private const int IS_PASSWORD = 31;
        private const int IS_ALPHANUMERIC_HALFWIDTH = 40;
        private const int IS_SEARCH = 50;
        private const int IS_NUMERIC_PIN = 64;

        public static void Apply(IntPtr hwnd, TextInputContentType contentType)
        {
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            try
            {
                SetInputScope(hwnd, Map(contentType));
            }
            catch (Exception)
            {
                // A refused or unavailable input scope only costs the hint.
            }
        }

        public static void Clear(IntPtr hwnd) => Apply(hwnd, TextInputContentType.Normal);

        private static int Map(TextInputContentType contentType) => contentType switch
        {
            TextInputContentType.Alpha => IS_ALPHANUMERIC_HALFWIDTH,
            TextInputContentType.Digits => IS_DIGITS,
            TextInputContentType.Pin => IS_NUMERIC_PIN,
            TextInputContentType.Number => IS_NUMBER,
            TextInputContentType.Email => IS_EMAIL_SMTPEMAILADDRESS,
            TextInputContentType.Url => IS_URL,
            TextInputContentType.Name => IS_PERSONALNAME_FULLNAME,
            TextInputContentType.Password => IS_PASSWORD,
            TextInputContentType.Search => IS_SEARCH,
            _ => IS_DEFAULT,
        };

        [DllImport("msctf.dll", ExactSpelling = true)]
        private static extern int SetInputScope(IntPtr hwnd, int inputScope);
    }
}
