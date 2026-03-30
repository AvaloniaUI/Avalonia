namespace Avalonia.Rendering.Composition.Drawing.Nodes;

internal class RenderDataRecordingItemListNode : IRenderDataItem
{
    public required RenderItemList Items { get; init; }

    public void Invoke(ref RenderDataNodeRenderContext context)
    {
        Items.Render(context.Context);
    }

    public Rect? Bounds => Items.Bounds;

    public bool HitTest(Point p) => Items.HitTest(p);
}
