using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Direct2D1
{
    public interface IExternalDirect2DRenderTargetSurface
    {
        SharpDX.Direct2D1.RenderTarget GetOrCreateRenderTarget();
        void DestroyRenderTarget();
        void BeforeDrawing();
        void AfterDrawing();
    }
}
