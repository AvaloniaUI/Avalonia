using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition.Transport
{
    partial class CompositionVisualChanges
    {
        public Change<ServerCompositionVisual> Parent;
        public Change<ServerCompositionTarget> Root;

        partial void ResetExtra()
        {
            Parent.Reset();
            Root.Reset();
        }
    }
}