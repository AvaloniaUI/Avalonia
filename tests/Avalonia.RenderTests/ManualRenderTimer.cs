using Avalonia.Rendering;
using System;

namespace Avalonia.Skia.RenderTests
{
    public class ManualRenderTimer : IRenderTimer
    {
        public Action<TimeSpan>? Tick { get; set; }
        public bool RunsInBackground => false;
        public void TriggerTick() => Tick?.Invoke(TimeSpan.Zero);
    }
}
