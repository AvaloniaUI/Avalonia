using System.Collections.Generic;
using Avalonia.Collections.Pooled;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Transport;

internal class DrawListVisualChanges : CompositionVisualChanges
{
    private CompositionDrawList? _drawCommands;

    public DrawListVisualChanges(IChangeSetPool pool) : base(pool)
    {
    }

    public CompositionDrawList? DrawCommands
    {
        get => _drawCommands;
        set
        {
            _drawCommands?.Dispose();
            _drawCommands = value;
            DrawCommandsIsSet = true;
        }
    }
    
    public bool DrawCommandsIsSet { get; private set; }

    public CompositionDrawList? AcquireDrawCommands()
    {
        var rv = _drawCommands;
        _drawCommands = null;
        DrawCommandsIsSet = false;
        return rv;
    }

    public override void Reset()
    {
        _drawCommands?.Dispose();
        _drawCommands = null;
        DrawCommandsIsSet = false;
        base.Reset();
    }

    public new static ChangeSetPool<DrawListVisualChanges> Pool { get; } =
        new ChangeSetPool<DrawListVisualChanges>(pool => new(pool));
}