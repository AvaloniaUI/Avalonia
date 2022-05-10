namespace Avalonia.Rendering.Composition.Transport;

partial class CompositionTargetChanges
{
    public Change<bool> RedrawRequested;

    partial void ResetExtra()
    {
        RedrawRequested.Reset();
    }
}