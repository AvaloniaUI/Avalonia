using System;

namespace Avalonia.Rendering.Composition;

partial class Compositor
{
    class CompositorRenderLoopTask : IRenderLoopTask
    {
        public bool NeedsUpdate { get; }
        public void Update(TimeSpan time)
        {
            throw new NotImplementedException();
        }

        public void Render()
        {
            throw new NotImplementedException();
        }
    }
}