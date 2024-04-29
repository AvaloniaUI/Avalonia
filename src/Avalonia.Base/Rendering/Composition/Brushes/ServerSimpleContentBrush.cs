using System;
using Avalonia.Media;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server;

internal sealed class ServerCompositionSimpleContentBrush : ServerCompositionSimpleTileBrush, ITileBrush, ISceneBrush
{
    private CompositionRenderDataSceneBrushContent? _content;

    internal ServerCompositionSimpleContentBrush(ServerCompositor compositor) : base(compositor)
    {
    }

    // TODO: Figure out something about disposable
    public ISceneBrushContent? CreateContent() => _content;

    protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
    {
        base.DeserializeChangesCore(reader, committedAt);
        _content = reader.ReadObject<CompositionRenderDataSceneBrushContent?>();
    }
}
