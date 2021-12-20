using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Rendering;

namespace Avalonia.Animation
{
    public class RenderLoopClock : ClockBase, IRenderLoopTask, IGlobalClock
    {
        protected override void Stop()
        {
            var loop = AvaloniaLocator.Current.GetService<IRenderLoop>() ??
                throw new InvalidOperationException("Unable to locate IRenderLoop.");
            loop.Remove(this);
        }

        bool IRenderLoopTask.NeedsUpdate => HasSubscriptions;

        void IRenderLoopTask.Render()
        {
        }

        void IRenderLoopTask.Update(TimeSpan time)
        {
            Pulse(time);
        }
    }
}
