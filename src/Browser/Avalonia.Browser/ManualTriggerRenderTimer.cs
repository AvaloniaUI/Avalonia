using System;
using System.Diagnostics;
using Avalonia.Rendering;

namespace Avalonia.Browser
{
    internal class ManualTriggerRenderTimer : IRenderTimer
    {
        private static readonly Stopwatch s_sw = Stopwatch.StartNew();

        public static ManualTriggerRenderTimer Instance { get; } = new();

        public void RaiseTick() => Tick?.Invoke(s_sw.Elapsed);

        public event Action<TimeSpan>? Tick;
        public bool RunsInBackground => false;
    }
}
