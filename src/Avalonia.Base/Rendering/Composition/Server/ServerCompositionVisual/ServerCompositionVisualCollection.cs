namespace Avalonia.Rendering.Composition.Server;

partial class ServerCompositionVisualCollection
{
    // Backlink to the owning visual, set in ServerCompositionVisual.Initialize.
    internal ServerCompositionVisual? Owner { get; set; }

    // Raises the owner's _childrenChanged flag, which the update walk turns into a whole-input re-ingest for
    // retained backdrops inside this subtree (their covering damage isn't in the working set at capture time).
    // This fires only from ServerList.DeserializeChangesCore's wholesale-replace branch, so it currently sees
    // EVERY children mutation. If ServerList is ever made incremental it must keep raising this on any child
    // add/remove/move, or backdrops nested under a structurally-changed container will silently under-repaint.
    protected override void OnListReplaced() => Owner?.NotifyChildrenChanged();
}
