using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Platform;

namespace Avalonia.X11.XShmExtensions;

internal class X11ShmFramebufferSurface : IFramebufferPlatformSurface
{
    public IFramebufferRenderTarget CreateFramebufferRenderTarget()
    {
        return new X11ShmImageSwapchain();
    }
}

internal class X11ShmImage
{
}

internal class X11ShmImageManager
{

}

class DeferredDisplayEvents
{

}

internal class X11ShmImageSwapchain : IFramebufferRenderTarget
{
    public void Dispose()
    {

    }

    public ILockedFramebuffer Lock()
    {
        throw new NotImplementedException();
    }
}
