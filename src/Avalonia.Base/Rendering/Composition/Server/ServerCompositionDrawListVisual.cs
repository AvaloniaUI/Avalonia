using System;
using System.Numerics;
using Avalonia.Collections.Pooled;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Server;

internal class ServerCompositionDrawListVisual : ServerCompositionContainerVisual
{
    private CompositionDrawList? _renderCommands;
    
    public ServerCompositionDrawListVisual(ServerCompositor compositor) : base(compositor)
    {
    }

    Rect? _contentBounds;

    public override Rect ContentBounds
    {
        get
        {
            if (_contentBounds == null)
            {
                var rect = Rect.Empty;
                if(_renderCommands!=null)
                    foreach (var cmd in _renderCommands)
                        rect = rect.Union(cmd.Item.Bounds);
                _contentBounds = rect;
            }

            return _contentBounds.Value;
        }
    }

    protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan commitedAt)
    {
        if (reader.Read<byte>() == 1)
        {
            _renderCommands?.Dispose();
            _renderCommands = reader.ReadObject<CompositionDrawList?>();
            _contentBounds = null;
        }
        base.DeserializeChangesCore(reader, commitedAt);
    }

    protected override void RenderCore(CompositorDrawingContextProxy canvas, Matrix4x4 transform)
    {
        if (_renderCommands != null)
        {
            _renderCommands.Render(canvas);
        }
        base.RenderCore(canvas, transform);
    }
}