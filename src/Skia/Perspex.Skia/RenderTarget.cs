using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Perspex.Media;
using Perspex.Platform;

namespace Perspex.Skia
{
    class RenderTarget : PerspexHandleHolder, IRenderTarget
    {
        public RenderTarget(IntPtr handle) : base(handle)
        {
        }

        protected override void Delete(IntPtr handle) => MethodTable.Instance.DisposeRenderTarget(handle);

        public DrawingContext CreateDrawingContext()
        {
            return
                new DrawingContext(
                    new DrawingContextImpl(MethodTable.Instance.RenderTargetCreateRenderingContext(Handle)));
        }
    }
}
