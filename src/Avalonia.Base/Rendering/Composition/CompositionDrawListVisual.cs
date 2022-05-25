using System.Numerics;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition;

internal class CompositionDrawListVisual : CompositionContainerVisual
{
    public Visual Visual { get; }

    private new DrawListVisualChanges Changes => (DrawListVisualChanges)base.Changes;
    private CompositionDrawList? _drawList;
    public CompositionDrawList? DrawList
    {
        get => _drawList;
        set
        {
            _drawList?.Dispose();
            _drawList = value;
            Changes.DrawCommands = value?.Clone();
        }
    }

    private protected override IChangeSetPool ChangeSetPool => DrawListVisualChanges.Pool;

    internal CompositionDrawListVisual(Compositor compositor, ServerCompositionDrawListVisual server, Visual visual) : base(compositor, server)
    {
        Visual = visual;
    }

    internal override bool HitTest(Point pt)
    {
        if (DrawList == null)
            return false;
        if (Visual is ICustomHitTest custom)
            return custom.HitTest(pt);
        foreach (var op in DrawList)
            if (op.Item.HitTest(pt))
                return true;
        return false;
    }
}