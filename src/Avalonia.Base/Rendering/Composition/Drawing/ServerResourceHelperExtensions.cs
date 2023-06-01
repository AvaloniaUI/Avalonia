using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Avalonia.Rendering.Composition.Drawing;

static class ServerResourceHelperExtensions
{
    public static IBrush? GetServer(this IBrush? brush, Compositor? compositor)
    {
        if (compositor == null)
            return brush;
        if (brush == null)
            return null;
        if (brush is IImmutableBrush immutable)
            return immutable;
        if (brush is ICompositionRenderResource<IBrush> resource)
            return resource.GetForCompositor(compositor);
        ThrowNotCompatible(brush);
        return null;
    }

    public static IPen? GetServer(this IPen? pen, Compositor? compositor)
    {
        if (compositor == null)
            return pen;
        if (pen == null)
            return null;
        if (pen is ImmutablePen immutable)
            return immutable;
        if (pen is ICompositionRenderResource<IPen> resource)
            return resource.GetForCompositor(compositor);
        ThrowNotCompatible(pen);
        return null;
    }

    [MethodImpl(MethodImplOptions.NoInlining), DoesNotReturn]
    static void ThrowNotCompatible(object o) =>
        throw new InvalidOperationException(o.GetType() + " is not compatible with composition");
    
    public static ITransform? GetServer(this ITransform? transform, Compositor? compositor)
    {
        if (compositor == null)
            return transform;
        if (transform == null)
            return null;
        if (transform is ImmutableTransform immutable)
            return immutable;
        if (transform is ICompositionRenderResource<ITransform> resource)
            resource.GetForCompositor(compositor);
        return new ImmutableTransform(transform.Value);
    }
}