// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Platform;
using Perspex.Win32.Interop;
using SharpDX;
using SharpDX.Direct2D1;
using DrawingContext = Perspex.Media.DrawingContext;
using DwFactory = SharpDX.DirectWrite.Factory;

namespace Perspex.Direct2D1
{
    public class RenderTarget : IRenderTarget
    {
        private readonly IntPtr _hwnd;
        private Size2 _savedSize;
        private Size2F _savedDpi;

        /// <summary>
        /// The render target.
        /// </summary>
        private readonly SharpDX.Direct2D1.RenderTarget _renderTarget;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderTarget"/> class.
        /// </summary>
        /// <param name="hwnd">The window handle.</param>
        public RenderTarget(IntPtr hwnd)
        {
            _hwnd = hwnd;
            Direct2DFactory = PerspexLocator.Current.GetService<Factory>();
            DirectWriteFactory = PerspexLocator.Current.GetService<DwFactory>();

            RenderTargetProperties renderTargetProperties = new RenderTargetProperties
            {
            };

            HwndRenderTargetProperties hwndProperties = new HwndRenderTargetProperties
            {
                Hwnd = hwnd,
                PixelSize = _savedSize = GetWindowSize(),
                PresentOptions = PresentOptions.Immediately,
            };

            _renderTarget = new WindowRenderTarget(
                Direct2DFactory,
                renderTargetProperties,
                hwndProperties);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderTarget"/> class.
        /// </summary>
        /// <param name="renderTarget">The render target.</param>
        public RenderTarget(SharpDX.Direct2D1.RenderTarget renderTarget)
        {
            Direct2DFactory = PerspexLocator.Current.GetService<Factory>();
            DirectWriteFactory = PerspexLocator.Current.GetService<DwFactory>();
            _renderTarget = renderTarget;
        }

        /// <summary>
        /// Gets the Direct2D factory.
        /// </summary>
        public Factory Direct2DFactory
        {
            get;
        }

        /// <summary>
        /// Gets the DirectWrite factory.
        /// </summary>
        public DwFactory DirectWriteFactory
        {
            get;
        }

        /// <summary>
        /// Creates a drawing context for a rendering session.
        /// </summary>
        /// <returns>An <see cref="Perspex.Media.DrawingContext"/>.</returns>
        public DrawingContext CreateDrawingContext()
        {
            var window = _renderTarget as WindowRenderTarget;

            if (window != null)
            {
                var size = GetWindowSize();
                var dpi = GetWindowDpi();

                if (size != _savedSize)
                {
                    window.Resize(_savedSize = size);
                }

                if (dpi != _savedDpi)
                {
                    window.DotsPerInch = _savedDpi = dpi;
                }
            }

            return new DrawingContext(new Media.DrawingContext(_renderTarget, DirectWriteFactory));
        }

        public void Dispose()
        {
            _renderTarget.Dispose();
        }

        private Size2F GetWindowDpi()
        {
            if (UnmanagedMethods.ShCoreAvailable)
            {
                uint dpix, dpiy;

                var monitor = UnmanagedMethods.MonitorFromWindow(
                    _hwnd,
                    UnmanagedMethods.MONITOR.MONITOR_DEFAULTTONEAREST);

                if (UnmanagedMethods.GetDpiForMonitor(
                        monitor,
                        UnmanagedMethods.MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI,
                        out dpix,
                        out dpiy) == 0)
                {
                    return new Size2F(dpix, dpiy);
                }
            }

            return new Size2F(96, 96);
        }

        private Size2 GetWindowSize()
        {
            UnmanagedMethods.RECT rc;
            UnmanagedMethods.GetClientRect(_hwnd, out rc);
            return new Size2(rc.right - rc.left, rc.bottom - rc.top);
        }
    }
}
