using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Controls.Platform.Surfaces
{
    public interface IFramebufferPlatformSurface
    {
        ILockedFramebuffer Lock();
    }
}
