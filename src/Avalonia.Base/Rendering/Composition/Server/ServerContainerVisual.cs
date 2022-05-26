using System.Numerics;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server
{
    internal partial class ServerCompositionContainerVisual : ServerCompositionVisual
    {
        public ServerCompositionVisualCollection Children { get; private set; } = null!;
        
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

        public override void Update(ServerCompositionTarget root, Matrix4x4 transform)
        {
            base.Update(root, transform);
            foreach (var child in Children) 
                child.Update(root, GlobalTransformMatrix);
        }

        partial void Initialize()
        {
            Children = new ServerCompositionVisualCollection(Compositor);
        }
    }
}