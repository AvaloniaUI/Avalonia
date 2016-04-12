using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Perspex.Media;
using Perspex.Platform;

namespace Perspex.Skia
{
/* This lives in shared project now, but we may need an android specific implementation anyways
 * 
    class RenderTarget : IRenderTarget
    {
        private IntPtr _currentRenderTarget = IntPtr.Zero;
        private Surface _currentSurface = null;
        private SurfaceView _view = null;

        public RenderTarget(IPlatformHandle handle)
        {
            _view = (SurfaceView) handle;
        }

        public void Dispose()
        {
            if (_currentRenderTarget != IntPtr.Zero)
            {
                MethodTable.Instance.DisposeRenderTarget(_currentRenderTarget);
                _currentRenderTarget = IntPtr.Zero;
                GC.SuppressFinalize(this);
            }
        }

        ~RenderTarget()
        {
            Dispose();
        }

        public DrawingContext CreateDrawingContext()
        {
            var surface = _view.Holder.Surface;
            if (surface == null)
                throw new InvalidOperationException("Surface isn't available");
            if (_currentSurface != surface)
            {
                _currentSurface = null;
                if (_currentRenderTarget != IntPtr.Zero)
                {
                    MethodTable.Instance.DisposeRenderTarget(_currentRenderTarget);
                    _currentRenderTarget = IntPtr.Zero;
                }

                _currentRenderTarget = MethodTable.Instance.CreateWindowRenderTarget(surface.Handle);
                _currentSurface = surface;
            }
            return new DrawingContext(
                new DrawingContextImpl(MethodTable.Instance.RenderTargetCreateRenderingContext(_currentRenderTarget)));
        }
    }
*/
}