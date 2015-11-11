using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Perspex.Media;
using Perspex.Platform;

namespace Perspex.Skia
{
#if !__ANDROID__
    class RenderTarget : PerspexHandleHolder, IRenderTarget
    {
        public RenderTarget(IPlatformHandle handle) : base(MethodTable.Instance.CreateWindowRenderTarget(handle.Handle))
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
#endif
}
