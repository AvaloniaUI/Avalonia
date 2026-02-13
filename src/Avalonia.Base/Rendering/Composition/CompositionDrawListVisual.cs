using Avalonia.Platform;
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
    private bool _childClipChanged;
    private bool _hasChildClip;
    private RoundedRect _childClip;
    private IGeometryImpl? _childClipGeometry;
    
    /// <summary>
    /// The list of drawing commands
    /// </summary>
    public CompositionRenderData? DrawList
    {
        get => _drawList;
        set
        {
            // Nothing to do
            if (value == null && _drawList == null)
                return;
            
            _drawList?.Dispose();
            _drawList = value;
            _drawListChanged = true;
            RegisterForSerialization();
        }
    }

    internal void SetChildClip(RoundedRect clip, IGeometryImpl? geometryClip)
    {
        if (_hasChildClip && _childClip.Equals(clip) && ReferenceEquals(_childClipGeometry, geometryClip))
            return;

        _hasChildClip = true;
        _childClip = clip;
        _childClipGeometry = geometryClip;
        _childClipChanged = true;
        RegisterForSerialization();
    }

    internal void ClearChildClip()
    {
        if (!_hasChildClip && _childClipGeometry == null)
            return;

        _hasChildClip = false;
        _childClip = default;
        _childClipGeometry = null;
        _childClipChanged = true;
        RegisterForSerialization();
    }

    private protected override void SerializeChangesCore(BatchStreamWriter writer)
    {
        writer.Write((byte)(_drawListChanged ? 1 : 0));
        if (_drawListChanged)
        {
            writer.WriteObject(DrawList?.Server);
            _drawListChanged = false;
        }
        writer.Write((byte)(_childClipChanged ? 1 : 0));
        if (_childClipChanged)
        {
            writer.Write(_hasChildClip);
            writer.Write(_childClip);
            writer.WriteObject(_childClipGeometry);
            _childClipChanged = false;
        }
        base.SerializeChangesCore(writer);
    }

    internal CompositionDrawListVisual(Compositor compositor, ServerCompositionDrawListVisual server, Visual visual) : base(compositor, server)
    {
        Visual = visual;
        CustomHitTestCountInSubTree = visual is ICustomHitTest ? 1 : 0;
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
