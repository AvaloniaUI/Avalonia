using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Server;

partial class ServerCompositionCacheMode
{
    private readonly WeakHashList<ServerCompositionVisual> _attachedVisuals = new();

    public void Subscribe(ServerCompositionVisual visual) => _attachedVisuals.Add(visual);

    public void Unsubscribe(ServerCompositionVisual visual) => _attachedVisuals.Remove(visual);

    protected override void ValuesInvalidated()
    {
        using var alive = _attachedVisuals.GetAlive();
        if (alive != null)
        {
            foreach (var v in alive.Span) 
                v.OnCacheModeStateChanged();
        }

        base.ValuesInvalidated();
    }
}