using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Threading;
using RemoteViewing.Vnc;
using RemoteViewing.Vnc.Server;

namespace Avalonia.Headless.Vnc
{
    public class HeadlessVncFramebufferSource : IVncFramebufferSource
    {
        public Window Window { get; set; }
        private object _lock = new object();
        public VncFramebuffer _framebuffer = new VncFramebuffer("Avalonia", 1, 1, VncPixelFormat.RGB32);

        private VncButton _previousButtons;
        public HeadlessVncFramebufferSource(VncServerSession session, Window window)
        {
            Window = window;
            session.PointerChanged += (_, args) =>
            {
                var pt = new Point(args.X, args.Y);
                    
                var buttons = (VncButton)args.PressedButtons;

                MouseButton TranslateButton(VncButton vncButton) =>
                    vncButton switch
                    {
                        VncButton.Left => MouseButton.Left,
                        VncButton.Middle => MouseButton.Middle,
                        VncButton.Right => MouseButton.Right,
                        _ => MouseButton.None
                    };

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

        public unsafe VncFramebuffer Capture()
        {
            lock (_lock)
            {
                using (var bmpRef = Window.CaptureRenderedFrame())
                {
                    if (bmpRef == null)
                        return _framebuffer;
                    var bmp = bmpRef;
                    if (bmp.PixelSize.Width != _framebuffer.Width || bmp.PixelSize.Height != _framebuffer.Height)
                    {
                        _framebuffer = new VncFramebuffer("Avalonia", bmp.PixelSize.Width, bmp.PixelSize.Height,
                            VncPixelFormat.RGB32);
                    }

                    var buffer = _framebuffer.GetBuffer();
                    fixed (byte* bufferPtr = buffer)
                    {
                        bmp.CopyPixels(new PixelRect(default, bmp.PixelSize), (IntPtr)bufferPtr, buffer.Length, _framebuffer.Stride);
                    }
                }
            }

            return _framebuffer;
        }
    }
}
