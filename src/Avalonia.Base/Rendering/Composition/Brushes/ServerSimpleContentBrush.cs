using System;
using Avalonia.Media;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server;

internal sealed class ServerCompositionSimpleContentBrush : ServerCompositionSimpleTileBrush, ITileBrush, ISceneBrush
{
    private CompositionRenderDataSceneBrushContent.Properties? _content;


    internal ServerCompositionSimpleContentBrush(ServerCompositor compositor) : base(compositor)
    {
    }

    public ISceneBrushContent? CreateContent() =>
        _content == null || _content.RenderData.IsDisposed
            ? null
            : new CompositionRenderDataSceneBrushContent(this, _content);

    protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
    {
        base.DeserializeChangesCore(reader, committedAt);
        var content = reader.ReadObject<CompositionRenderDataSceneBrushContent.Properties?>();

        if (!ReferenceEquals(_content?.RenderData, content?.RenderData))
        {
            _content?.RenderData.RemoveObserver(this);
            content?.RenderData.AddObserver(this);
        }

        _content = content;
    }

    public override void Dispose()
    {
        _content?.RenderData.RemoveObserver(this);
        base.Dispose();
    }
}
