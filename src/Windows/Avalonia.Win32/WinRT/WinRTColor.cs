using System.Runtime.InteropServices;

namespace Avalonia.Win32.WinRT
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct WinRTColor
    {
        public byte A;
        public byte R;
        public byte G;
        public byte B;

        public static WinRTColor FromArgb(byte a, byte r, byte g, byte b) => new WinRTColor()
        {
            A = a, R = r, G = g, B = b
        };
    }
}
