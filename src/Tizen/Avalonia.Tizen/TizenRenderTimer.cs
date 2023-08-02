using System.Diagnostics;
using Avalonia.Rendering;

namespace Avalonia.Tizen;
internal class TizenRenderTimer : IRenderTimer
{
    private Stopwatch _st = Stopwatch.StartNew();
    public bool RunsInBackground => true;

    public event Action<TimeSpan> Tick;

    internal void Render()
    {
        Tick?.Invoke(_st.Elapsed);
    }
}
