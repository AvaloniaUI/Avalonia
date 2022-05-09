using System.Numerics;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server
{
    class ServerCustomDrawVisual<TData> : ServerCompositionContainerVisual
    {
        private readonly ICustomDrawVisualRenderer<TData> _renderer;
        private TData? _data;
        public ServerCustomDrawVisual(ServerCompositor compositor, ICustomDrawVisualRenderer<TData> renderer) : base(compositor)
        {
            _renderer = renderer;
        }

        protected override void ApplyCore(ChangeSet changes)
        {
            var c = (CustomDrawVisualChanges<TData>) changes;
            if (c.Data.IsSet)
                _data = c.Data.Value;
            
            base.ApplyCore(changes);
        }

        protected override void RenderCore(CompositorDrawingContextProxy canvas, Matrix4x4 transform)
        {
            _renderer.Render(canvas, _data);
            base.RenderCore(canvas, transform);
        }
    }
}