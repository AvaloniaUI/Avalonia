using System;
using Avalonia.Metadata;

namespace Avalonia.Rendering
{
    [Unstable]
    public interface IDeferredRendererLock
    {
        IDisposable? TryLock();
    }
}
