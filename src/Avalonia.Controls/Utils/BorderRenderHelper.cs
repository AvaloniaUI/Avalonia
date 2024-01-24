using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Controls.Utils
{
    /// <summary>
    /// Contains helper methods for rendering a <see cref="Border"/>'s background and border to a given context.
    /// </summary>
    internal class BorderRenderHelper
    {
        private bool _useComplexRendering;
        private bool? _backendSupportsIndividualCorners;
        private StreamGeometry? _backgroundGeometryCache;
        private StreamGeometry? _borderGeometryCache;
        private Size _size;
        private Thickness _borderThickness;
        private CornerRadius _cornerRadius;
        private BackgroundSizing _backgroundSizing;
        private bool _initialized;
        private IPen? _cachedPen;

        private void Update(Size finalSize, Thickness borderThickness, CornerRadius cornerRadius, BackgroundSizing backgroundSizing)
        {
            _backendSupportsIndividualCorners ??= AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>()
                .SupportsIndividualRoundRects;
            _size = finalSize;
            _borderThickness = borderThickness;
            _cornerRadius = cornerRadius;
            _backgroundSizing = backgroundSizing;
            _initialized = true;

            if (borderThickness.IsUniform &&
                (cornerRadius.IsUniform || _backendSupportsIndividualCorners == true) &&
                backgroundSizing == BackgroundSizing.CenterBorder)
            {
                _backgroundGeometryCache = null;
                _borderGeometryCache = null;
                _useComplexRendering = false;
            }
            else
            {
                _useComplexRendering = true;

                var boundRect = new Rect(finalSize);
                var innerRect = boundRect.Deflate(borderThickness);
                StreamGeometry? backgroundGeometry = null;

                if (innerRect.Width != 0 && innerRect.Height != 0)
                {
                    backgroundGeometry = new StreamGeometry();
                    var backgroundOuterKeypoints = GeometryBuilder.CalculateRoundedCornersRectangleWinUI(
                        boundRect,
                        borderThickness,
                        cornerRadius,
                        backgroundSizing);

                    using (var ctx = backgroundGeometry.Open())
                    {
                        GeometryBuilder.DrawRoundedCornersRectangle(ctx, ref backgroundOuterKeypoints);
                    }

                    _backgroundGeometryCache = backgroundGeometry;
                }
                else
                {
                    _backgroundGeometryCache = null;
                }

                if (boundRect.Width != 0 && boundRect.Height != 0)
                {
                    var borderGeometry = new StreamGeometry();
                    var borderInnerKeypoints = GeometryBuilder.CalculateRoundedCornersRectangleWinUI(
                        boundRect,
                        borderThickness,
                        cornerRadius,
                        BackgroundSizing.InnerBorderEdge);
                    var borderOuterKeypoints = GeometryBuilder.CalculateRoundedCornersRectangleWinUI(
                        boundRect,
                        borderThickness,
                        cornerRadius,
                        BackgroundSizing.OuterBorderEdge);

                    using (var ctx = borderGeometry.Open())
                    {
                        GeometryBuilder.DrawRoundedCornersRectangle(ctx, ref borderOuterKeypoints);

                        if (backgroundGeometry != null)
                        {
                            GeometryBuilder.DrawRoundedCornersRectangle(ctx, ref borderInnerKeypoints);
                        }
                    }

                    _borderGeometryCache = borderGeometry;
                }
                else
                {
                    _borderGeometryCache = null;
                }
            }
        }

        public void Render(
            DrawingContext context,
            Size finalSize,
            Thickness borderThickness,
            CornerRadius cornerRadius,
            BackgroundSizing backgroundSizing,
            IBrush? background,
            IBrush? borderBrush,
            BoxShadows boxShadows)
        {
            if (_size != finalSize
                || _borderThickness != borderThickness
                || _cornerRadius != cornerRadius
                || _backgroundSizing != backgroundSizing
                || !_initialized)
            {
                Update(finalSize, borderThickness, cornerRadius, backgroundSizing);
            }

            if (_useComplexRendering)
            {
                var backgroundGeometry = _backgroundGeometryCache;
                if (backgroundGeometry != null)
                {
                    context.DrawGeometry(background, null, backgroundGeometry);
                }

                var borderGeometry = _borderGeometryCache;
                if (borderGeometry != null)
                {
                    context.DrawGeometry(borderBrush, null, borderGeometry);
                }
            }
            else
            {
                var thickness = _borderThickness.Top;

                Pen.TryModifyOrCreate(ref _cachedPen, borderBrush, thickness);

                var rect = new Rect(_size);
                if (!MathUtilities.IsZero(thickness))
                    rect = rect.Deflate(thickness * 0.5);
                var rrect = new RoundedRect(rect, _cornerRadius.TopLeft, _cornerRadius.TopRight,
                    _cornerRadius.BottomRight, _cornerRadius.BottomLeft);

                context.DrawRectangle(background, _cachedPen, rrect, boxShadows);
            }
        }
    }
}
