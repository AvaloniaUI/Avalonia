using Avalonia.Rendering.Composition.Server;

// Special license applies <see href="https://raw.githubusercontent.com/AvaloniaUI/Avalonia/master/src/Avalonia.Base/Rendering/Composition/License.md">License.md</see>

namespace Avalonia.Rendering.Composition
{
    /// <summary>
    /// A node in the visual tree that can have children.
    /// </summary>
    public partial class CompositionContainerVisual : CompositionVisual
    {
        public CompositionVisualCollection Children { get; private set; } = null!;

        partial void InitializeDefaultsExtra()
        {
            Children = new CompositionVisualCollection(this, Server.Children);
        }

        private protected override void OnRootChangedCore()
        {
            foreach (var ch in Children)
                ch.Root = Root;
            base.OnRootChangedCore();
        }
    }
}
