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
    /// Appends the working set, in layer pixels, to <paramref name="buffer"/>. Does not finalize, inflate or
    /// merge anything.
    /// </summary>
    void CopyWorkingSetTo(List<LtrbRect> buffer);

    void CopyRects(List<PixelRect> rects);
    // Dirty rectangles before antialiasing inflation, for replay through AddRect/FinalizeFrame.
    void CopyUninflatedRects(List<LtrbRect> rects);
}

internal interface IDirtyRectCollector
{
    void AddRect(LtrbRect rect);

    /// <summary>
    /// The working set of the layer that this collector belongs to.
    /// </summary>
    DirtyRectWorkingSet GetWorkingSet();
}
