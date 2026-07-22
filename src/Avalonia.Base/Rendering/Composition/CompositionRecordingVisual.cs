using System;
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
    private bool _recordingChanged;

    internal CompositionRecordingVisual(Compositor compositor, ServerCompositionRecordingVisual server)
        : base(compositor, server)
    {
    }

    /// <summary>
    /// The recording rendered by this visual, drawn behind any child visuals.
    /// Must be bound to the same <see cref="Compositor"/> as the visual; the
    /// caller keeps ownership and must keep the recording alive (and undisposed)
    /// while it is assigned.
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
                if (!value.IsCompositorBound || value.Compositor != Compositor)
                    throw new ArgumentException(
                        "The recording must be bound to the same compositor as the visual.",
                        nameof(value));
                value.EnsureRegisteredForSerialization();
            }

            _recording = value;
            _recordingChanged = true;
            RegisterForSerialization();
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

    private protected override void SerializeChangesCore(BatchStreamWriter writer)
    {
        writer.Write((byte)(_recordingChanged ? 1 : 0));
        if (_recordingChanged)
        {
            writer.WriteObject(_recording?.RenderData?.Server);
            _recordingChanged = false;
        }

        base.SerializeChangesCore(writer);
    }

    internal override bool HitTest(Point pt) => _recording?.HitTest(pt) ?? false;
}
