using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Controls.Platform;
using Avalonia.FreeDesktop.DBusSystemTray;
using Avalonia.Platform;

namespace Avalonia.X11
{
    class X11TrayIconImpl : ITrayIconImpl
    {
        private readonly AvaloniaX11Platform _avaloniaX11Platform;
        public INativeMenuExporter MenuExporter { get; }

        public Action OnClicked { get; set; }
        private SNIDBus sni = new SNIDBus();

        public X11TrayIconImpl(AvaloniaX11Platform avaloniaX11Platform)
        {
            _avaloniaX11Platform = avaloniaX11Platform;
            sni.Initialize();
            MenuExporter = sni.NativeMenuExporter;
        }

        public void Dispose()
        {
            sni?.Dispose();
        }

        [StructLayout(LayoutKind.Explicit)]
        readonly struct BGRA32
        {
            [FieldOffset(3)] public readonly byte A;

            [FieldOffset(2)] public readonly byte R;

            [FieldOffset(1)] public readonly byte G;

            [FieldOffset(0)] public readonly byte B;

            public ARGB32 ToARGB32()
            {
                return new ARGB32(A, R, G, B);
            }
        }


        [StructLayout(LayoutKind.Explicit)]
        readonly struct ARGB32
        {
            [FieldOffset(0)] public readonly byte A;

            [FieldOffset(1)] public readonly byte R;

            [FieldOffset(2)] public readonly byte G;

            [FieldOffset(3)] public readonly byte B;

            public ARGB32(byte a, byte r, byte g, byte b)
            {
                A = a;
                R = r;
                G = g;
                B = b;
            }
        }

        public void SetIcon(IWindowIconImpl icon)
        {
            if (!(icon is X11IconData x11icon)) return;

            var w = (int)x11icon.Data[0];
            var h = (int)x11icon.Data[1];

            var rx = x11icon.Data.AsSpan(2);
            var pixLength = w * h;
            var pixelArray = new ARGB32[pixLength];

            for (var i = 0; i < pixLength; i++)
            {
                var u = rx[i].ToUInt32();
                var a = (byte)((u & 0xFF000000) >> 24);
                var r = (byte)((u & 0x00FF0000) >> 16);
                var g = (byte)((u & 0x0000FF00) >> 8);
                var b = (byte)((u & 0x000000FF));
                pixelArray[i] = new ARGB32(a, r, g, b);
            }

            var pixmapBytes = MemoryMarshal.Cast<ARGB32, byte>(pixelArray).ToArray();

            sni.SetIcon(new Pixmap(w, h, pixmapBytes));
        }
 
        public void SetIsVisible(bool visible)
        {
        }

        public void SetToolTipText(string text)
        {
        }
    }
}
