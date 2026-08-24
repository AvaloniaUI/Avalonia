using Avalonia.Media;

namespace Avalonia.Rendering.Composition.Drawing;

internal interface IRenderDataPayload<TSelf> where TSelf : unmanaged, IRenderDataPayload<TSelf>
{
    static abstract RenderDataOpcode Opcode { get; }
}

internal struct DrawLinePayload : IRenderDataPayload<DrawLinePayload>
{
    public static RenderDataOpcode Opcode => RenderDataOpcode.DrawLine;

    public int ServerPen;
    public int ClientPen;
    public Point P1;
    public Point P2;
}

internal struct DrawRectanglePayload : IRenderDataPayload<DrawRectanglePayload>
{
    public static RenderDataOpcode Opcode => RenderDataOpcode.DrawRectangle;

    public int ServerBrush;
    public int ServerPen;
    public int ClientPen;
    public RoundedRect Rect;
    public int BoxShadowCount;
}

internal struct DrawEllipsePayload : IRenderDataPayload<DrawEllipsePayload>
{
    public static RenderDataOpcode Opcode => RenderDataOpcode.DrawEllipse;

    public int ServerBrush;
    public int ServerPen;
    public int ClientPen;
    public Rect Rect;
}

internal struct DrawGeometryPayload : IRenderDataPayload<DrawGeometryPayload>
{
    public static RenderDataOpcode Opcode => RenderDataOpcode.DrawGeometry;

    public int ServerBrush;
    public int ServerPen;
    public int ClientPen;
    public int Geometry;
}

internal struct DrawGlyphRunPayload : IRenderDataPayload<DrawGlyphRunPayload>
{
    public static RenderDataOpcode Opcode => RenderDataOpcode.DrawGlyphRun;

    public int ServerBrush;
    public int GlyphRun;
}

internal struct DrawBitmapPayload : IRenderDataPayload<DrawBitmapPayload>
{
    public static RenderDataOpcode Opcode => RenderDataOpcode.DrawBitmap;

    public int Bitmap;
    public double Opacity;
    public Rect SourceRect;
    public Rect DestRect;
}

internal struct DrawCustomPayload : IRenderDataPayload<DrawCustomPayload>
{
    public static RenderDataOpcode Opcode => RenderDataOpcode.DrawCustom;

    public int Operation;
}

internal struct PushClipPayload : IRenderDataPayload<PushClipPayload>
{
    public static RenderDataOpcode Opcode => RenderDataOpcode.PushClip;

    public RoundedRect Clip;
}

internal struct PushGeometryClipPayload : IRenderDataPayload<PushGeometryClipPayload>
{
    public static RenderDataOpcode Opcode => RenderDataOpcode.PushGeometryClip;

    public int Geometry;
}

internal struct PushOpacityPayload : IRenderDataPayload<PushOpacityPayload>
{
    public static RenderDataOpcode Opcode => RenderDataOpcode.PushOpacity;

    public double Opacity;
}

internal struct PushOpacityMaskPayload : IRenderDataPayload<PushOpacityMaskPayload>
{
    public static RenderDataOpcode Opcode => RenderDataOpcode.PushOpacityMask;

    public int Brush;
    public Rect Bounds;
}

internal struct PushTransformPayload : IRenderDataPayload<PushTransformPayload>
{
    public static RenderDataOpcode Opcode => RenderDataOpcode.PushTransform;

    public Matrix Matrix;
}

internal struct PushRenderOptionsPayload : IRenderDataPayload<PushRenderOptionsPayload>
{
    public static RenderDataOpcode Opcode => RenderDataOpcode.PushRenderOptions;

    public RenderOptions Options;
}

internal struct PushTextOptionsPayload : IRenderDataPayload<PushTextOptionsPayload>
{
    public static RenderDataOpcode Opcode => RenderDataOpcode.PushTextOptions;

    public TextOptions Options;
}

internal struct PushEffectPayload : IRenderDataPayload<PushEffectPayload>
{
    public static RenderDataOpcode Opcode => RenderDataOpcode.PushEffect;

    public int Effect;
    public Rect Bounds;
}
