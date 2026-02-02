using System;
using System.Collections.Generic;
using System.Numerics;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Reactive;

namespace Avalonia.Rendering.Composition.Server;

internal class SingleDirtyRectTracker : IDirtyRectTracker
{
    private LtrbRect? _rect;
    private LtrbRect _extendedRect;
    
    private readonly Random _random = new();
    public void AddRect(LtrbRect rect)
    {
        _rect = LtrbRect.FullUnion(_rect, rect);
    }

    public LtrbRect? UninflatedCombinedIntersect(LtrbRect rect) => _rect?.IntersectOrNull(rect);
    public bool UninflatedIntersects(LtrbRect rect) => _rect?.Intersects(rect) ?? false;

    public void FinalizeFrame(LtrbRect bounds)
    {

        _extendedRect = _rect.HasValue
            ? LtrbPixelRect.FromRectUnscaled(_rect.Value.Inflate(new Thickness(1)).IntersectOrEmpty(bounds))
                .ToLtrbRectUnscaled()
            : default;
    }

    public IDisposable BeginDraw(IDrawingContextImpl ctx)
    {
        ctx.PushClip(_extendedRect.ToRect());
        return Disposable.Create(ctx.PopClip);
    }

    public bool IsEmpty => _rect?.IsZeroSize ?? true;
    public bool Intersects(LtrbRect rect) => _extendedRect.Intersects(rect);

    public void Initialize(LtrbRect bounds) => _rect = default;
    public void Visualize(IDrawingContextImpl context)
    {
        context.DrawRectangle(
            new ImmutableSolidColorBrush(
                new Color(30, (byte)_random.Next(255), (byte)_random.Next(255), (byte)_random.Next(255))),
            null, _extendedRect.ToRect());
    }

    public LtrbRect CombinedRect => _extendedRect;
}