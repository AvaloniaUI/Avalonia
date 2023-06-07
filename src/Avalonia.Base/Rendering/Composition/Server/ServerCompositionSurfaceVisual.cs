using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Server;

internal partial class ServerCompositionSurfaceVisual
{
    protected override void RenderCore(CompositorDrawingContextProxy canvas, Rect currentTransformedClip)
    {
        if (Surface == null)
            return;
        if (Surface.Bitmap == null)
            return;
        var bmp = Surface.Bitmap.Item;

        //TODO: add a way to always render the whole bitmap instead of just assuming 96 DPI
        canvas.DrawBitmap(Surface.Bitmap.Item, 1, new Rect(bmp.PixelSize.ToSize(1)), new Rect(
            new Size(Size.X, Size.Y)));
    }


    private void OnSurfaceInvalidated() => ValuesInvalidated();

    protected override void OnAttachedToRoot(ServerCompositionTarget target)
    {
        if (Surface != null)
            Surface.Changed += OnSurfaceInvalidated;
        base.OnAttachedToRoot(target);
    }

    protected override void OnDetachedFromRoot(ServerCompositionTarget target)
    {
        if (Surface != null)
            Surface.Changed -= OnSurfaceInvalidated;
        base.OnDetachedFromRoot(target);
    }

    partial void OnSurfaceChanged()
    {
        if (Surface != null)
            Surface.Changed += OnSurfaceInvalidated;
    }

    partial void OnSurfaceChanging()
    {
        if (Surface != null)
            Surface.Changed -= OnSurfaceInvalidated;
    }
}
