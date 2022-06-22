using Avalonia.Rendering;
using System.Threading.Tasks;
using System;


#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests
#endif
{
    public class ManualRenderTimer : IRenderTimer
    {
        public event Action<TimeSpan> Tick;
        public bool RunsInBackground => false;
        public void TriggerTick() => Tick?.Invoke(TimeSpan.Zero);
        public Task TriggerBackgroundTick() => Task.Run(TriggerTick);
    }
}