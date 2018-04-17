using System;

namespace Avalonia.Gtk3
{
    public interface IDeferredRenderOperation : IDisposable
    {
        void RenderNow(IntPtr? ctx);
    }
}