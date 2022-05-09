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

    internal CompositionDrawListVisual(Compositor compositor, ServerCompositionContainerVisual server, Visual visual) : base(compositor, server)
    {
        Visual = visual;
    }

    internal override bool HitTest(Vector2 point)
    {
        if (DrawList == null)
            return false;
        var pt = new Point(point.X, point.Y);
        if (Visual is ICustomHitTest custom)
            return custom.HitTest(pt);
        foreach (var op in DrawList)
            if (op.Item.HitTest(pt))
                return true;
        return false;
    }
}