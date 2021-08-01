using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using RemoteViewing.Vnc;
using RemoteViewing.Vnc.Server;

namespace Avalonia.Headless.Vnc
{
    public class HeadlessVncFramebufferSource : IVncFramebufferSource
    {
        public IHeadlessWindow Window { get; set; }
        private object _lock = new object();
        public VncFramebuffer _framebuffer = new VncFramebuffer("Avalonia", 1, 1, VncPixelFormat.RGB32);

        private VncButton _previousButtons;
        public HeadlessVncFramebufferSource(VncServerSession session, Window window)
        {
            Window = (IHeadlessWindow)window.PlatformImpl;
            session.PointerChanged += (_, args) =>
            {
                var pt = new Point(args.X, args.Y);
                    
                var buttons = (VncButton)args.PressedButtons;

                int TranslateButton(VncButton vncButton) =>
                    vncButton == VncButton.Left ? 0 : vncButton == VncButton.Right ? 1 : 2;

                var modifiers = (RawInputModifiers)(((int)buttons & 7) << 4);
                
                Dispatcher.UIThread.Post(() =>
                {
                    Window?.MouseMove(pt);
                    foreach (var btn in CheckedButtons)
                        if (_previousButtons.HasAllFlags(btn) && !buttons.HasAllFlags(btn))
                            Window?.MouseUp(pt, TranslateButton(btn), modifiers);
                    
                    foreach (var btn in CheckedButtons)
                        if (!_previousButtons.HasAllFlags(btn) && buttons.HasAllFlags(btn))
                            Window?.MouseDown(pt, TranslateButton(btn), modifiers);
                    _previousButtons = buttons;
                }, DispatcherPriority.Input);
            };
            
        }

        [Flags]
        enum VncButton
        {
            Left = 1,
            Middle = 2,
            Right = 4,
            ScrollUp = 8,
            ScrollDown = 16
        }
        

        private static VncButton[] CheckedButtons = new[] {VncButton.Left, VncButton.Middle, VncButton.Right}; 

        public VncFramebuffer Capture()
        {
            lock (_lock)
            {
                using (var bmpRef = Window.GetLastRenderedFrame())
                {
                    if (bmpRef?.Item == null)
                        return _framebuffer;
                    var bmp = bmpRef.Item;
                    if (bmp.PixelSize.Width != _framebuffer.Width || bmp.PixelSize.Height != _framebuffer.Height)
                    {
                        _framebuffer = new VncFramebuffer("Avalonia", bmp.PixelSize.Width, bmp.PixelSize.Height,
                            VncPixelFormat.RGB32);
                    }

                    using (var fb = bmp.Lock())
                    {
                        var buf = _framebuffer.GetBuffer();
                        if (_framebuffer.Stride == fb.RowBytes)
                            Marshal.Copy(fb.Address, buf, 0, buf.Length);
                        else
                            for (var y = 0; y < fb.Size.Height; y++)
                            {
                                var sourceStart = fb.RowBytes * y;
                                var dstStart = _framebuffer.Stride * y;
                                var row = fb.Size.Width * 4;
                                Marshal.Copy(new IntPtr(sourceStart + fb.Address.ToInt64()), buf, dstStart, row);
                            }
                    }
                }
            }

            return _framebuffer;
        }
    }
}
