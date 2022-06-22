using System.Numerics;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server
{
    /// <summary>
    /// Server-side counterpart of <see cref="CompositionContainerVisual"/>.
    /// Mostly propagates update and render calls, but is also responsible
    /// for updating adorners in deferred manner
    /// </summary>
    internal partial class ServerCompositionContainerVisual : ServerCompositionVisual
    {
        public ServerCompositionVisualCollection Children { get; private set; } = null!;
        
        protected override void RenderCore(CompositorDrawingContextProxy canvas)
        {
            base.RenderCore(canvas);

            foreach (var ch in Children)
            {
                ch.Render(canvas);
            }
        }

        public override void Update(ServerCompositionTarget root, Matrix4x4 transform)
        {
            base.Update(root, transform);
            foreach (var child in Children)
            {
                if (child.AdornedVisual != null)
                    root.EnqueueAdornerUpdate(child);
                else
                    child.Update(root, GlobalTransformMatrix);
            }
        }

        partial void Initialize()
        {
            Children = new ServerCompositionVisualCollection(Compositor);
        }
    }
}