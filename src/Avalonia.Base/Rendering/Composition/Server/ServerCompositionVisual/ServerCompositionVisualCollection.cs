namespace Avalonia.Rendering.Composition.Server;

partial class ServerCompositionVisualCollection
{
    // Set in ServerCompositionVisual.Initialize.
    internal ServerCompositionVisual? Owner { get; set; }

    // The update walk marks the whole input dirty for retained backdrop visuals below a visual with changed
    // children. ServerList replaces the whole list on every change, so this is called for every add, remove and
    // move. If ServerList becomes incremental, it still has to call this for each of them.
    protected override void OnListReplaced() => Owner?.NotifyChildrenChanged();
}
