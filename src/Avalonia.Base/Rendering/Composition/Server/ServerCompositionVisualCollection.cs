namespace Avalonia.Rendering.Composition.Server
{
    partial class ServerCompositionVisualCollection
    {
        protected override void OnBeforeListClear()
        {
            foreach (var child in List)
                child.AddTransformedSubTreeBoundsToParentDirtyRect();
        }
    }
}
