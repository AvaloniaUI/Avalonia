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
}

internal interface IDirtyRectCollector
{
    void AddRect(LtrbRect rect);
    LtrbRect? UninflatedCombinedIntersect(LtrbRect rect);
    bool UninflatedIntersects(LtrbRect rect);
    
}