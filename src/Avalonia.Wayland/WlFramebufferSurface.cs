using System;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Platform;

namespace Avalonia.Wayland
{
    public class WlFramebufferSurface : IFramebufferPlatformSurface
    {
        public ILockedFramebuffer Lock()
        {
            throw new NotImplementedException();
        }
    }
}
