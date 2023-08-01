using System;
using System.Runtime.InteropServices;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Native.Interop;
using Avalonia.Platform;

namespace Avalonia.Native
{
    internal unsafe class DeferredFramebuffer : ILockedFramebuffer
    {
        private readonly IAvnSoftwareRenderTarget _renderTarget;
        private readonly Action<Action<IAvnWindowBase>> _lockWindow;
        
        public DeferredFramebuffer(IAvnSoftwareRenderTarget renderTarget, Action<Action<IAvnWindowBase>> lockWindow,
                                   int width, int height, Vector dpi)
        {
            _renderTarget = renderTarget;
            _lockWindow = lockWindow;
            Address = Marshal.AllocHGlobal(width * height * 4);
            Size = new PixelSize(width, height);
            RowBytes = width * 4;
            Dpi = dpi;
            Format = PixelFormat.Bgra8888;
        }

        public IntPtr Address { get; set; }
        public PixelSize Size { get; set; }
        public int Height { get; set; }
        public int RowBytes { get; set; }
        public Vector Dpi { get; set; }
        public PixelFormat Format { get; set; }

        public void Dispose()
        {
            if (Address == IntPtr.Zero)
                return;

            _lockWindow(win =>
            {
                var fb = new AvnFramebuffer
                {
                    Data = Address.ToPointer(),
                    Dpi = new AvnVector { X = Dpi.X, Y = Dpi.Y },
                    Width = Size.Width,
                    Height = Size.Height,
                    PixelFormat = (AvnPixelFormat)Format.FormatEnum,
                    Stride = RowBytes
                };

                _renderTarget.SetFrame(&fb);

            });
            
            Marshal.FreeHGlobal(Address);

            Address = IntPtr.Zero;
        }
    }
}
