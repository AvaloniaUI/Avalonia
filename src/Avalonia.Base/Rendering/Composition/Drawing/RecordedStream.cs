using System;

namespace Avalonia.Rendering.Composition.Drawing;

/// <summary>
/// The stream of an immutable <see cref="DrawingRecording"/>, shared through
/// <see cref="Avalonia.Utilities.IRef{T}"/> handles. The recording holds one handle and every
/// stream that embeds the recording takes its own, so the bitmaps, glyph runs and custom draw
/// operations the stream references are released only once the last holder is disposed. That
/// can happen on the render thread, after a replay that outlived the recording itself.
/// </summary>
internal sealed class RecordedStream : IDisposable
{
    public RecordedStream(RenderDataStream stream) => Stream = stream;

    public RenderDataStream Stream { get; }

    public void Dispose()
    {
        Stream.DisposeResources();
        Stream.Dispose();
    }
}
