using Avalonia.Rendering;
using System.Threading.Tasks;
using System;

namespace Avalonia.Skia.RenderTests
{
    public class ManualRenderTimer : IRenderTimer
    {
        public event Action<TimeSpan> Tick;
        public bool RunsInBackground => false;
        public void TriggerTick() => Tick?.Invoke(TimeSpan.Zero);
        public Task TriggerBackgroundTick() => Task.Run(TriggerTick);
    }
}
