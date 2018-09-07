using System;
using System.Threading.Tasks;

namespace Avalonia.Rendering
{
    public interface IRenderLoopTask
    {
        bool NeedsUpdate { get; }
        void Update(long tickCount);
        void Render();
    }

    public class MockRenderLoopTask : IRenderLoopTask
    {
        public bool NeedsUpdate => true;

        public void Render()
        {
        }

        public void Update(long tickCount)
        {
        }
    }
}
