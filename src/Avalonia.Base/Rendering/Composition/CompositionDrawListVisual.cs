using System;
using System.Numerics;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.Composition;


/// <summary>
/// A composition visual that holds a list of drawing commands issued by <see cref="Avalonia.Visual"/>
/// </summary>
internal class CompositionDrawListVisual : CompositionContainerVisual
{
    /// <summary>
    /// The associated <see cref="Avalonia.Visual"/>
    /// </summary>
    public Visual Visual { get; }

    private bool _drawListChanged;
    private CompositionDrawList? _drawList;
    
    /// <summary>
    /// The list of drawing commands
    /// </summary>
    public CompositionDrawList? DrawList
    {
        get => _drawList;
        set
        {
            _drawList?.Dispose();
            _drawList = value;
            _drawListChanged = true;
            RegisterForSerialization();
        }
    }

    private protected override void SerializeChangesCore(BatchStreamWriter writer)
    {
        writer.Write((byte)(_drawListChanged ? 1 : 0));
        if (_drawListChanged)
        {
            writer.WriteObject(DrawList?.Clone());
            _drawListChanged = false;
        }
        base.SerializeChangesCore(writer);
    }

    internal CompositionDrawListVisual(Compositor compositor, ServerCompositionDrawListVisual server, Visual visual) : base(compositor, server)
    {
        Visual = visual;
    }

    internal override bool HitTest(Point pt, Func<IVisual, bool>? filter)
    {
        var custom = Visual as ICustomHitTest;
        if (DrawList == null && custom == null)
            return false;
        if (filter != null && !filter(Visual))
            return false;
        if (custom != null)
        {
            // Simulate the old behavior
            // TODO: Change behavior once legacy renderers are removed
            pt += new Point(Offset.X, Offset.Y);
            return custom.HitTest(pt);
        }

        foreach (var op in DrawList!)
            if (op.Item.HitTest(pt))
                return true;
        return false;
    }
}