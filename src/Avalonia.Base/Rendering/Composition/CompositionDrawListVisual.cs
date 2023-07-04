using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

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
    private CompositionRenderData? _drawList;
    
    /// <summary>
    /// The list of drawing commands
    /// </summary>
    public CompositionRenderData? DrawList
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
            writer.WriteObject(DrawList?.Server);
            _drawListChanged = false;
        }
        base.SerializeChangesCore(writer);
    }

    internal CompositionDrawListVisual(Compositor compositor, ServerCompositionDrawListVisual server, Visual visual) : base(compositor, server)
    {
        Visual = visual;
    }

    internal override bool HitTest(Point pt)
    {
        var custom = Visual as ICustomHitTest;
        if (DrawList == null && custom == null)
            return false;
        if (custom != null)
        {
            return custom.HitTest(pt);
        }

        return DrawList?.HitTest(pt) ?? false;
    }
}
