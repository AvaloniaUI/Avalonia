namespace Avalonia.Media;

/// <summary>
/// Describes whether an enclosing <see cref="Avalonia.Rendering.Composition.DrawingRecording"/>
/// takes responsibility for disposing a child recording referenced via
/// <see cref="DrawingContext.DrawRecording(Avalonia.Rendering.Composition.DrawingRecording, DrawingRecordingOwnership)"/>.
/// The distinction is honored only when the enclosing <see cref="DrawingContext"/> is being
/// used to build a <see cref="Avalonia.Rendering.Composition.DrawingRecording"/>; replay
/// contexts ignore ownership.
/// </summary>
public enum DrawingRecordingOwnership
{
    /// <summary>
    /// The enclosing recording does not dispose the child. The external owner must
    /// dispose it independently. Suitable for shared sub-recordings — e.g. a symbol
    /// library referenced from many places.
    /// </summary>
    Shared,

    /// <summary>
    /// The enclosing recording disposes the child when the enclosing recording is
    /// disposed. Suitable for fire-and-forget sub-recordings created inline during
    /// the record delegate and never referenced externally.
    /// </summary>
    Owned
}
