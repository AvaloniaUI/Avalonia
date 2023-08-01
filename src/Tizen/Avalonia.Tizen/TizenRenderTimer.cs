using Avalonia.Rendering;

namespace Avalonia.Tizen;
internal class TizenRenderTimer : IRenderTimer
{
    public bool RunsInBackground => false;

    public event Action<TimeSpan> Tick;

    internal void Render()
    {
        Tick?.Invoke(TimeSpan.FromMilliseconds(Environment.TickCount));
    }
}
