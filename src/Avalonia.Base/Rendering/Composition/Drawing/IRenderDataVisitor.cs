using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Drawing;

internal interface IRenderDataVisitor<TScope> where TScope : unmanaged
{
    bool StopVisiting { get; }

    void OnDrawLine(IPen? serverPen, IPen? clientPen, Point p1, Point p2);
    void OnDrawRectangle(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, RoundedRect rect, BoxShadows boxShadows);
    void OnDrawEllipse(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, Rect rect);
    void OnDrawGeometry(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, IGeometryImpl? geometry);
    void OnDrawGlyphRun(IBrush? serverBrush, IRef<IGlyphRunImpl>? glyphRun);
    void OnDrawBitmap(IRef<IBitmapImpl>? bitmap, double opacity, Rect sourceRect, Rect destRect);
    void OnDrawCustom(ICustomDrawOperation? operation);

    TScope OnPushClip(RoundedRect clip);
    TScope OnPushGeometryClip(IGeometryImpl? geometry);
    TScope OnPushOpacity(double opacity);
    TScope OnPushOpacityMask(IBrush? brush, Rect bounds);
    TScope OnPushTransform(Matrix matrix);
    TScope OnPushRenderOptions(RenderOptions options);
    TScope OnPushTextOptions(TextOptions options);
    void OnPop(in TScope scope);
}
