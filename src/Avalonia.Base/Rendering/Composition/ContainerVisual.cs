using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition
{
    public class CompositionContainerVisual : CompositionVisual
    {
        public CompositionVisualCollection Children { get; }
        internal CompositionContainerVisual(Compositor compositor, ServerCompositionContainerVisual server) : base(compositor, server)
        {
            Children = new CompositionVisualCollection(this, server.Children);
        }

        private protected override void OnRootChanged()
        {
            foreach (var ch in Children)
                ch.Root = Root;
            base.OnRootChanged();
        }
    }
}