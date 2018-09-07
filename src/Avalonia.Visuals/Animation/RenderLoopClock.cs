using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Rendering;

namespace Avalonia.Animation
{
    public class RenderLoopClock : Clock, IRenderLoopTask
    {
        bool IRenderLoopTask.NeedsUpdate => HasSubscriptions;

        void IRenderLoopTask.Render()
        {
        }

        void IRenderLoopTask.Update(long tickCount)
        {
            Pulse(tickCount);
        }
    }
}
