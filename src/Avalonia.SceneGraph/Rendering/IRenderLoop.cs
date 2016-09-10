using System;

namespace Avalonia.Rendering
{
    public interface IRenderLoop
    {
        event EventHandler<EventArgs> Tick;
    }
}