using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Drawing;

internal partial class RenderDataStream
{
    internal struct BoundsScope
    {
        public Rect? SavedBounds;
        public bool IsTransform;
        public Matrix Matrix;
    }

    internal struct BoundsVisitor : IRenderDataVisitor<BoundsScope>
    {
        public Rect? Current;

        public bool StopVisiting => false;

        public void OnDrawLine(IPen? serverPen, IPen? clientPen, Point p1, Point p2)
        {
            if (serverPen != null)
                Current = Rect.Union(Current, LineBoundsHelper.CalculateBounds(p1, p2, serverPen));
        }

        public void OnDrawRectangle(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, RoundedRect rect,
            BoxShadows boxShadows)
        {
            var bounds = boxShadows.TransformBounds(rect.Rect)
                .Inflate((serverPen?.Thickness ?? 0) / 2);
            Current = Rect.Union(Current, bounds);
        }

        public void OnDrawEllipse(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, Rect rect)
            => Current = Rect.Union(Current, rect.Inflate(serverPen?.Thickness ?? 0));

        public void OnDrawGeometry(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, IGeometryImpl? geometry)
            => Current = Rect.Union(Current, geometry?.GetRenderBounds(serverPen) ?? default);

        public void OnDrawGlyphRun(IBrush? serverBrush, IRef<IGlyphRunImpl>? glyphRun)
            => Current = Rect.Union(Current, glyphRun?.Item?.Bounds ?? default);

        public void OnDrawBitmap(IRef<IBitmapImpl>? bitmap, double opacity, Rect sourceRect, Rect destRect)
            => Current = Rect.Union(Current, destRect);

        public void OnDrawCustom(ICustomDrawOperation? operation)
            => Current = Rect.Union(Current, operation?.Bounds);

        private BoundsScope EnterChildScope(bool isTransform = false, Matrix matrix = default)
        {
            var scope = new BoundsScope { SavedBounds = Current, IsTransform = isTransform, Matrix = matrix };
            Current = null;
            return scope;
        }

        public BoundsScope OnPushClip(RoundedRect clip) => EnterChildScope();
        public BoundsScope OnPushGeometryClip(IGeometryImpl? geometry) => EnterChildScope();
        public BoundsScope OnPushOpacity(double opacity) => EnterChildScope();
        public BoundsScope OnPushOpacityMask(IBrush? brush, Rect bounds) => EnterChildScope();
        public BoundsScope OnPushTransform(Matrix matrix) => EnterChildScope(true, matrix);
        public BoundsScope OnPushRenderOptions(RenderOptions options) => EnterChildScope();
        public BoundsScope OnPushTextOptions(TextOptions options) => EnterChildScope();

        public void OnPop(in BoundsScope scope)
        {
            var childUnion = Current;
            if (scope.IsTransform)
                childUnion = childUnion?.TransformToAABB(scope.Matrix);
            Current = Rect.Union(scope.SavedBounds, childUnion);
        }
    }

    public Rect? CalculateBounds()
    {
        var visitor = new BoundsVisitor();
        Visit<BoundsVisitor, BoundsScope>(ref visitor);
        return visitor.Current;
    }
}
