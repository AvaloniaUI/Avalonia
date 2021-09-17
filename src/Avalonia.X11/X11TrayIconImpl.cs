using System;
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
        public INativeMenuExporter MenuExporter => null;

        public Action OnClicked { get; set; }
        private SNIDBus sni = new SNIDBus();

        public X11TrayIconImpl(AvaloniaX11Platform avaloniaX11Platform)
        {
            _avaloniaX11Platform = avaloniaX11Platform;
            sni.Initialize();
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
        }

        static unsafe class X11IconToPixmap
        {
        }

        public void SetIcon(IWindowIconImpl icon)
        {
            if (icon is X11IconData x11icon)
            {
                unsafe
                {
                    using var l = x11icon.Lock();

                    if (l.Format != PixelFormat.Bgra8888) return;
                    var h = l.Size.Height;
                    var w = l.Size.Width;
                    
                    var totalPixels = w * h;
                    var totalBytes = totalPixels * 4;
                    var _bgraBuf = new BGRA32[totalPixels];
                    var byteBuf = new byte[totalBytes];

                    fixed (void* src = &x11icon.Data[0])
                    fixed (void* dst = &_bgraBuf[0])
                        Buffer.MemoryCopy(src, dst, (uint)totalBytes, (uint)totalBytes);

                    var byteCount = 0;
                    
                    foreach (var curPix in _bgraBuf)
                    {
                        byteBuf[byteCount++] = curPix.A;
                        byteBuf[byteCount++] = curPix.R;
                        byteBuf[byteCount++] = curPix.G;
                        byteBuf[byteCount++] = curPix.B;
                    }
                    
                    sni.SetIcon(new Pixmap(w, h, byteBuf));
                }
            }

            ;
        }

        public void SetIsVisible(bool visible)
        {
        }

        public void SetToolTipText(string text)
        {
        }
    }
}
