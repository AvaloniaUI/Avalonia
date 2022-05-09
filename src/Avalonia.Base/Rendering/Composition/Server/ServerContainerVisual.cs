using System.Numerics;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server
{
    internal class ServerCompositionContainerVisual : ServerCompositionVisual
    {
        public ServerCompositionVisualCollection Children { get; }
        
        
        protected override void RenderCore(CompositorDrawingContextProxy canvas, Matrix4x4 transform)
        {
            base.RenderCore(canvas, transform);

            foreach (var ch in Children)
            {
                var t = transform;

                t = ch.CombinedTransformMatrix * t;
                ch.Render(canvas, t);
            }
        }

        public ServerCompositionContainerVisual(ServerCompositor compositor) : base(compositor)
        {
            Children = new ServerCompositionVisualCollection(compositor);
        }
    }
}