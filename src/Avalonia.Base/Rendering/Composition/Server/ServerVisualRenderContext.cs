using System;
using System.Collections.Generic;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

internal class ServerVisualRenderContext
{
    public IDirtyRectTracker? DirtyRects { get; }
    public bool DetachedRendering { get; }
    public CompositorDrawingContextProxy Canvas { get; }
    private readonly Stack<Matrix>? _transformStack;

    public ServerVisualRenderContext(CompositorDrawingContextProxy canvas, IDirtyRectTracker? dirtyRects,
        bool detachedRendering)
    {
        Canvas = canvas;
        DirtyRects = dirtyRects;
        DetachedRendering = detachedRendering;
        if (detachedRendering)
        {
            _transformStack = new();
            _transformStack.Push(canvas.Transform);
        }
    }


    public bool ShouldRender(ServerCompositionVisual visual, LtrbRect currentTransformedClip)
    {
        if (DetachedRendering)
            return true;
        if (currentTransformedClip.IsZeroSize)
            return false;
        if (DirtyRects?.Intersects(currentTransformedClip) == false)
            return false;
        return true;
    }

    public bool ShouldRenderOwnContent(ServerCompositionVisual visual, LtrbRect currentTransformedClip)
    {
        if (DetachedRendering)
            return true;
        return currentTransformedClip.Intersects(visual.TransformedOwnContentBounds)
               && DirtyRects?.Intersects(visual.TransformedOwnContentBounds) != false;
    }

    public RestoreTransform SetOrPushTransform(ServerCompositionVisual visual)
    {
        if (!DetachedRendering)
        {
            Canvas.Transform = visual.GlobalTransformMatrix;
            return default;
        }
        else
        {
            var transform = visual.CombinedTransformMatrix * _transformStack!.Peek();
            Canvas.Transform = transform;
            _transformStack.Push(transform);
            return new RestoreTransform(this);
        }
    }

    public struct RestoreTransform(ServerVisualRenderContext? parent) : IDisposable
    {
        public void Dispose()
        {
            if (parent != null)
            {
                parent._transformStack!.Pop();
                parent.Canvas.Transform = parent._transformStack.Peek();
            }
        }
    }
    
}