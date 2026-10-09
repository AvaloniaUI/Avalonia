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
        public Thickness EffectPadding;
    }

    internal struct BoundsVisitor : IRenderDataVisitor<BoundsScope>
    {
        public Rect? Current;
        private readonly bool _useClientResources;
        private readonly Matrix _outerTransform;
        private readonly bool _hasOuterTransform;
        private int _depth;

        public BoundsVisitor(bool useClientResources, Matrix outerTransform)
        {
            _useClientResources = useClientResources;
            _outerTransform = outerTransform;
            _hasOuterTransform = !outerTransform.IsIdentity;
        }

        public bool StopVisiting => false;

        // Server pen instances are compositor shadows: they are only valid on the
        // render thread and only after a commit applied, so synchronous UI-thread
        // queries read the live client pen instead.
        private IPen? EffectivePen(IPen? serverPen, IPen? clientPen)
            => _useClientResources ? clientPen : serverPen;

        // The optional outer transform is applied per top-level entry (a draw op
        // or a whole push scope) instead of once to the final union, which keeps
        // rotated and skewed bounds tighter for multi-item content.
        private void Union(Rect? bounds)
        {
            if (_hasOuterTransform && _depth == 0)
                bounds = bounds?.TransformToAABB(_outerTransform);
            Current = Rect.Union(Current, bounds);
        }

        public void OnDrawLine(IPen? serverPen, IPen? clientPen, Point p1, Point p2)
        {
            var pen = EffectivePen(serverPen, clientPen);
            if (pen != null)
                Union(LineBoundsHelper.CalculateBounds(p1, p2, pen));
        }

        public void OnDrawRectangle(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, RoundedRect rect,
            BoxShadows boxShadows)
        {
            var bounds = boxShadows.TransformBounds(rect.Rect)
                .Inflate((EffectivePen(serverPen, clientPen)?.Thickness ?? 0) / 2);
            Union(bounds);
        }

        public void OnDrawEllipse(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, Rect rect)
            => Union(rect.Inflate(EffectivePen(serverPen, clientPen)?.Thickness ?? 0));

        public void OnDrawGeometry(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, IGeometryImpl? geometry)
            => Union(geometry?.GetRenderBounds(EffectivePen(serverPen, clientPen)) ?? default);

        public void OnDrawGlyphRun(IBrush? serverBrush, IRef<IGlyphRunImpl>? glyphRun)
            => Union(glyphRun?.Item?.Bounds ?? default);

        public void OnDrawBitmap(IRef<IBitmapImpl>? bitmap, double opacity, Rect sourceRect, Rect destRect)
            => Union(destRect);

        public void OnDrawCustom(ICustomDrawOperation? operation)
            => Union(operation?.Bounds);

        public void OnDrawRecording(ServerCompositionRenderData? server, CompositionRenderData? client,
            RenderDataStream? stream, Matrix transform)
        {
            Rect? bounds;
            if (client != null)
            {
                // Compositor-bound child: client-side queries answer from the
                // client data, which is valid before any commit; the render
                // thread answers from the server data, whose bounds follow
                // resource changes without a re-record.
                bounds = _useClientResources
                    ? client.GetBounds(transform)
                    : TransformedServerBounds(server, transform);
            }
            else if (stream != null)
            {
                bounds = ServerCompositionRenderData.ApplyRenderBoundsRounding(
                    stream.CalculateBounds(_useClientResources, transform));
            }
            else
                bounds = null;

            Union(bounds);
        }

        private static Rect? TransformedServerBounds(ServerCompositionRenderData? server, Matrix transform)
        {
            if (server?.Bounds?.ToRect() is not { } bounds)
                return null;
            return transform.IsIdentity ? bounds : bounds.TransformToAABB(transform);
        }

        private BoundsScope EnterChildScope(bool isTransform = false, Matrix matrix = default,
            Thickness effectPadding = default)
        {
            var scope = new BoundsScope
            {
                SavedBounds = Current, IsTransform = isTransform, Matrix = matrix, EffectPadding = effectPadding
            };
            Current = null;
            _depth++;
            return scope;
        }

        public BoundsScope OnPushClip(RoundedRect clip) => EnterChildScope();
        public BoundsScope OnPushGeometryClip(IGeometryImpl? geometry) => EnterChildScope();
        public BoundsScope OnPushOpacity(double opacity) => EnterChildScope();
        public BoundsScope OnPushOpacityMask(IBrush? brush, Rect bounds) => EnterChildScope();
        public BoundsScope OnPushTransform(Matrix matrix) => EnterChildScope(true, matrix);
        public BoundsScope OnPushRenderOptions(RenderOptions options) => EnterChildScope();
        public BoundsScope OnPushTextOptions(TextOptions options) => EnterChildScope();

        public BoundsScope OnPushEffect(IEffect? effect, Rect bounds)
            => EnterChildScope(effectPadding: effect.GetEffectOutputPadding());

        public void OnPop(in BoundsScope scope)
        {
            _depth--;
            var childUnion = Current;
            if (scope.IsTransform)
                childUnion = childUnion?.TransformToAABB(scope.Matrix);
            else if (childUnion.HasValue && !scope.EffectPadding.Equals(default))
                childUnion = childUnion.Value.Inflate(scope.EffectPadding);
            Current = scope.SavedBounds;
            Union(childUnion);
        }
    }

    public Rect? CalculateBounds()
        => CalculateBounds(useClientResources: false, Matrix.Identity);

    public Rect? CalculateBounds(bool useClientResources, Matrix outerTransform)
    {
        var visitor = new BoundsVisitor(useClientResources, outerTransform);
        Visit<BoundsVisitor, BoundsScope>(ref visitor);
        return visitor.Current;
    }
}
