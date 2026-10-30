using System;
using Avalonia.Metadata;

namespace Avalonia.Platform;

/// <summary>
/// Identifies reusable buffer contents. Equality and hash codes must remain stable after invalidation.
/// </summary>
[PrivateApi]
public interface IPartialPresentationBuffer : IEquatable<IPartialPresentationBuffer>
{
    /// <summary>
    /// Gets whether the buffer can still be reused. Invalidation is permanent.
    /// </summary>
    bool IsValid { get; }
}

/// <summary>
/// Provides partial presentation for one output drawing context. The context owns this feature.
/// </summary>
[PrivateApi]
public interface IPartialPresentationFeature
{
    /// <summary>
    /// Gets the acquired buffer identity, or null if its contents cannot be tracked.
    /// An unfamiliar or invalid identity requires a full repaint.
    /// </summary>
    IPartialPresentationBuffer? Buffer { get; }

    /// <summary>
    /// Declares dirty rectangles once, before drawing to the output buffer. Rectangles use device pixels and a top-left origin.
    /// The caller must repaint every pixel in bufferDirtyRects. Implementations must copy spans needed after this call.
    /// </summary>
    /// <param name="frameDirtyRects">Changes since the previous successful presentation.</param>
    /// <param name="bufferDirtyRects">The complete region that will be repainted in this buffer.</param>
    void SetDirtyRects(ReadOnlySpan<PixelRect> frameDirtyRects, ReadOnlySpan<PixelRect> bufferDirtyRects);

    /// <summary>
    /// Marks drawing as complete. Call after the last drawing command, before disposing the context.
    /// Without this call, disposal cancels presentation and invalidates buffer contents.
    /// </summary>
    void Succeeded();
}
