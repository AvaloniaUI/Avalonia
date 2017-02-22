using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Controls.Platform.Surfaces
{
    public interface IPlatformWindowRenderSurface
    {
        IntPtr Handle { get; }
        CancellationToken Disposed { get; }
    }
}
