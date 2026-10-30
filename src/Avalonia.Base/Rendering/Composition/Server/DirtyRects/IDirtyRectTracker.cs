using System;
using System.Collections.Generic;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

internal interface IDirtyRectTracker : IDirtyRectCollector
{
    /// <summary>
    /// Post-processes the dirty rect area (e. g. to account for anti-aliasing)
    /// </summary>
    void FinalizeFrame(LtrbRect bounds);
    IDisposable BeginDraw(IDrawingContextImpl ctx);
    bool IsEmpty { get; }
    bool Intersects(LtrbRect rect);
    void Initialize(LtrbRect bounds);
    void Visualize(IDrawingContextImpl context);
    LtrbRect CombinedRect { get; }

    /// <summary>
    /// Appends the tracker's currently accumulating working set (raw, in tracker space) to
    /// <paramref name="buffer"/> without finalizing, inflating or optimizing it. Safe to call mid-pass.
    /// </summary>
    void CollectWorkingSet(List<LtrbRect> buffer);
}

internal interface IDirtyRectCollector
{
    void AddRect(LtrbRect rect);

    /// <summary>
    /// Resolves a side-effect-free view of the underlying tracker's working set plus the host-local ↔
    /// tracker-space mapping, for the update walk's backdrop capture and descent gate.
    /// </summary>
    DirtyRectWorkingSet GetWorkingSet();
}