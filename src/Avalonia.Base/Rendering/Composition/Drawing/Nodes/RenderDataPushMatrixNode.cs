namespace Avalonia.Rendering.Composition.Drawing.Nodes;

class RenderDataPushMatrixNode : RenderDataPushNode
{
    public Matrix Matrix { get; set; }

    public override void Push(ref RenderDataNodeRenderContext context)
    {
        var current = context.Context.Transform;
        context.MatrixStack.Push(current);
        context.Context.Transform = Matrix * current;
    }

    public override void Pop(ref RenderDataNodeRenderContext context)
    {
        context.Context.Transform = context.MatrixStack.Pop();
    }

    public override bool HitTest(Point p)
    {
        if (Matrix.TryInvert(out var inverted))
            return base.HitTest(p.Transform(inverted));
        return false;
    }

    public override Rect? Bounds => base.Bounds?.TransformToAABB(Matrix);
}