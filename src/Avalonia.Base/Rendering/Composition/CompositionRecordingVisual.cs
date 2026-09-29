using System;
using Avalonia.Media;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition;

/// <summary>
/// A composition visual that renders a compositor-bound
/// <see cref="DrawingRecording"/> behind its children. Combined with the
/// animatable <see cref="CompositionVisual"/> properties (Offset, Scale,
/// RotationAngle, CenterPoint, Opacity) and composition animations this hosts
/// retained content that moves entirely on the render thread — no per-frame
/// work on the UI thread. Created via
/// <see cref="Compositor.CreateRecordingVisual()"/>.
/// </summary>
public class CompositionRecordingVisual : CompositionContainerVisual
{
    private DrawingRecording? _recording;
    private CompositionRenderData? _content;
    private bool _contentChanged;

    internal CompositionRecordingVisual(Compositor compositor, ServerCompositionRecordingVisual server)
        : base(compositor, server)
    {
        // The visual's bounds come from its recording and it is never given a Size, so
        // clipping to its own bounds would clip to an empty rect and cull the visual.
        ClipToBounds = false;
    }

    /// <summary>
    /// The recording rendered by this visual, drawn behind any child visuals.
    /// Must be bound to the same <see cref="Compositor"/> as the visual and not disposed;
    /// the caller keeps ownership. While the visual is part of a composition tree it holds its
    /// own reference to the recording's content, so disposing the recording while it is
    /// assigned leaves the visual displaying it until the visual leaves the tree or is given
    /// another recording.
    /// </summary>
    public DrawingRecording? Recording
    {
        get => _recording;
        set
        {
            if (ReferenceEquals(_recording, value))
                return;

            if (value != null)
            {
                if (value.IsDisposed)
                    throw new ObjectDisposedException(
                        nameof(DrawingRecording), "Cannot assign a disposed DrawingRecording.");
                if (!value.IsCompositorBound || value.Compositor != Compositor)
                    throw new ArgumentException(
                        "The recording must be bound to the same compositor as the visual.",
                        nameof(value));
            }

            _recording = value;
            UpdateContent();
        }
    }

    /// <summary>
    /// A static transform applied to this visual's content and children before
    /// the animatable Scale/RotationAngle/Offset properties. Useful for fixed
    /// coordinate-space mappings around animated content.
    /// </summary>
    public Matrix Transform
    {
        get => TransformMatrix;
        set => TransformMatrix = value;
    }

    private protected override void OnRootChangedCore()
    {
        base.OnRootChangedCore();
        UpdateContent();
    }

    // Holds a counted reference to the recording's content only while the visual is part of a
    // composition tree, because composition visuals are never disposed: leaving the tree is the
    // one point a dropped visual is sure to pass, so that is where the reference is released.
    // A recording disposed in the meantime is not picked up again on re-entering the tree.
    private void UpdateContent()
    {
        var wanted = Root != null && _recording is { IsDisposed: false } recording ? recording.RenderData : null;
        if (ReferenceEquals(wanted, _content))
            return;

        if (wanted != null)
        {
            _recording!.EnsureRegisteredForSerialization();
            wanted.AddRef();
        }

        _content?.Dispose();
        _content = wanted;
        _contentChanged = true;
        RegisterForSerialization();
    }

    private protected override void SerializeChangesCore(BatchStreamWriter writer)
    {
        writer.Write((byte)(_contentChanged ? 1 : 0));
        if (_contentChanged)
        {
            writer.WriteObject(_content?.Server);
            _contentChanged = false;
        }

        base.SerializeChangesCore(writer);
    }

    internal override bool HitTest(Point pt) => _content?.HitTest(pt) ?? false;

    internal override IntersectionResult HitTest(Geometry geometry) =>
        _content?.HitTest(geometry) ?? IntersectionResult.Empty;
}
