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
        
        protected override void RenderCore(CompositorDrawingContextProxy canvas, Rect currentTransformedClip)
        {
            base.RenderCore(canvas, currentTransformedClip);

            foreach (var ch in Children)
            {
                ch.Render(canvas, currentTransformedClip);
            }
        }

        public override void Update(ServerCompositionTarget root)
        {
            base.Update(root);
            foreach (var child in Children)
            {
                if (child.AdornedVisual != null)
                    root.EnqueueAdornerUpdate(child);
                else
                    child.Update(root);
            }

            IsDirtyComposition = false;
        }

        partial void Initialize()
        {
            Children = new ServerCompositionVisualCollection(Compositor);
        }
    }
}