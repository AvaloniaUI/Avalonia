using System;
using System.IO;
using System.Linq;
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

            public BGRA32(byte a, byte r, byte g, byte b)
            {
                A = a;
                R = r;
                G = g;
                B = b;
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

        static unsafe class X11IconToPixmap
        {
        }

        public void SetIcon(IWindowIconImpl icon)
        {
            if (icon is X11IconData x11icon)
            {
                var w = 6;
                var h = 6;
                var rb = 4;
                var pixelBuf = new ARGB32[w * h];
                
                 
                var gold = new ARGB32(255, 212, 175, 55);
                var red = new ARGB32(255, 255, 0, 0);
                var blue = new ARGB32(255, 0, 0, 255);

                var ix = 0;
                for (var y = 0; y < h; y++)
                {
                    var offset = y * w;
                    for (var x = 0; x < w; x++)
                    {
                        pixelBuf[offset + x] = (ix % 2 == 1) ? gold : blue;
                        ix++;
                    }
                    ix++;
                }

                var pixmapBytes = MemoryMarshal.Cast<ARGB32, byte>(pixelBuf.AsSpan()).ToArray();
                
                sni.SetIcon(new Pixmap(w, h, pixmapBytes));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int PixCoord(int x, int y, int w) => x + (y *w);

        public void SetIsVisible(bool visible)
        {
        }

        public void SetToolTipText(string text)
        {
        }
    }
}
