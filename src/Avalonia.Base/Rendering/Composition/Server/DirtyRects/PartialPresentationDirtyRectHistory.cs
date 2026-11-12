using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

// Each output buffer can contain an older scene revision. This history supplies the
// scene changes needed to bring that buffer up to date, in addition to the current frame.
// Buffer repairs stay separate from scene changes, so repairs do not propagate to other buffers.
// The dirty rectangle tracker still handles merging, antialiasing inflation, clipping and culling.
internal sealed class PartialPresentationDirtyRectHistory
{
    private const int MaxHistory = 16;
    // Each entry contains scene dirty rectangles before antialiasing inflation.
    private readonly Queue<(ulong Revision, LtrbRect[] Rects)> _history = new();
    
    // Maps each buffer identity to its last completed scene revision. Buffer order can vary.
    private readonly Dictionary<IPartialPresentationBuffer, ulong> _buffers = new();
    
    // Pooled scratch buffer
    private readonly List<IPartialPresentationBuffer> _expired = new();
    
    // These lists hold the pending frame information, captured by BeforeDraw/BeforeTargetDraw
    private readonly List<LtrbRect> _frameUninflatedDirtyRects = new();
    private readonly List<PixelRect> _frameDirtyRects = new();
    private readonly List<PixelRect> _renderTargetBufferDirtyRects = new();
    // The feature belongs to the current drawing context. AfterDraw releases this reference.
    private IPartialPresentationFeature? _partialPresentation;
    private IPartialPresentationBuffer? _buffer;
    private ulong _revision;

    /// <summary>
    /// Finalized scene changes since the previous frame, converted to output coordinates by BeforeTargetDraw.
    /// This value is in SURFACE coordinates accounting for rotation 
    /// </summary>
    public ReadOnlySpan<PixelRect> FrameDirtyRects => CollectionsMarshal.AsSpan(_frameDirtyRects);
    
    /// <summary>
    /// All pixels that need repainting in output coordinates, including repairs from BeforeTargetDraw.
    /// This value is in SURFACE coordinates accounting for rotation 
    /// </summary>
    public ReadOnlySpan<PixelRect> BufferDirtyRects => CollectionsMarshal.AsSpan(_renderTargetBufferDirtyRects);

    /// <summary>
    /// This hook needs to be called before any drawing operations, including offscreen ones
    /// It captures various information about the current frame
    /// </summary>
    public void BeforeDraw(IPartialPresentationFeature partialPresentation, IDirtyRectTracker tracker)
    {
        // A buffer needs every revision after its last draw. BeforeDraw removes identities
        // with invalid contents or missing history, so BeforeTargetDraw requests a full repaint.
        _expired.Clear();
        foreach (var pair in _buffers)
            if (!pair.Key.IsValid || pair.Value < _revision - (ulong)_history.Count)
                _expired.Add(pair.Key);
        foreach (var expired in _expired)
            _buffers.Remove(expired);
        _expired.Clear();

        _partialPresentation = partialPresentation;
        _buffer = partialPresentation.Buffer;
        // Replay uses uninflated rectangles to prevent repeated antialiasing inflation.
        // The finalized pixel rectangles describe this frame to the presentation backend.
        tracker.CopyUninflatedRects(_frameUninflatedDirtyRects);
        tracker.CopyRects(_frameDirtyRects);
    }

    /// <summary>
    /// This hook needs to be called before drawing to the actual render target
    /// It expands the current dirty rect tracker to contain all the rects we actually need
    /// to repaint and passes those to partial presentation feature
    /// </summary>
    public void BeforeTargetDraw(IDirtyRectTracker tracker, LtrbRect bounds, Matrix? outputTransform = null)
    {
        var partialPresentation = _partialPresentation
            ?? throw new InvalidOperationException("BeforeDraw must be called before BeforeTargetDraw.");

        // Trackers can cache their finalized regions. Rebuilding from the saved scene
        // dirty rectangles lets finalization include all rectangles from the current frame and history.
        tracker.Initialize(bounds);

        // An unfamiliar or invalid buffer needs a full repaint. Otherwise, its contents
        // already include its recorded revision, so only later scene changes need replay.
        if (_buffer is not { IsValid: true } || !_buffers.TryGetValue(_buffer, out var revision))
            tracker.AddRect(bounds);
        else
        {
            foreach (var rect in _frameUninflatedDirtyRects)
                tracker.AddRect(rect);

            foreach (var frame in _history)
                if (frame.Revision > revision)
                    foreach (var rect in frame.Rects)
                        tracker.AddRect(rect);
        }

        // Finalization merges scene changes and repairs with the same tracker policy.
        // The default GPU tracker still produces one rectangle for clipping and culling.
        tracker.FinalizeFrame(bounds);
        tracker.CopyRects(_renderTargetBufferDirtyRects);
        // History and scene culling stay in scene coordinates. Only the output rectangles need rotation.
        if (outputTransform is { IsIdentity: false } transform)
        {
            TransformRects(_frameDirtyRects, transform);
            TransformRects(_renderTargetBufferDirtyRects, transform);
        }
        // The backend receives the rectangles before the first clear or draw to the output buffer.
        partialPresentation.SetDirtyRects(FrameDirtyRects, BufferDirtyRects);
    }

    private static void TransformRects(List<PixelRect> rects, Matrix transform)
    {
        for (var c = 0; c < rects.Count; c++)
            rects[c] = PixelRect.FromRect(rects[c].ToRect(1).TransformToAABB(transform), 1);
    }

    /// <summary>
    /// Cleanup and history recording hook
    /// </summary>
    public void AfterSuccessfulDraw()
    {
        _partialPresentation = null;
        _history.Enqueue((++_revision, _frameUninflatedDirtyRects.ToArray()));
        if (_history.Count > MaxHistory)
            _history.Dequeue();
        // Disposal can invalidate the buffer. Only a valid buffer can represent this revision.
        if (_buffer is { IsValid: true })
            _buffers[_buffer] = _revision;
    }
}
