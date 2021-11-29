using System;
using System.Diagnostics;
using Avalonia.Rendering;

namespace Avalonia.Web.Blazor
{
    public class ManualTriggerRenderTimer : IRenderTimer
    {
        private static readonly Stopwatch _sw = Stopwatch.StartNew();

        public static ManualTriggerRenderTimer Instance { get; } = new ManualTriggerRenderTimer();

        public void RaiseTick() => Tick?.Invoke(_sw.Elapsed);

        public event Action<TimeSpan> Tick;
    }
}