using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Direct2D1
{
    public interface IExternalDirect2DRenderTargetSurface
    {
        SharpDX.Direct2D1.RenderTarget CreateRenderTarget();
        void BeforeDrawing();
        void AfterDrawing();
    }
}
