using System;
using System.Buffers;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Drawing;

internal partial class RenderDataStream
{
    private const int MaxStackScopeDepth = 64;

    public void Visit<TVisitor, TScope>(ref TVisitor visitor)
        where TVisitor : struct, IRenderDataVisitor<TScope>
        where TScope : unmanaged
    {
        var reader = new RenderDataReader(_writer.Written);
        TScope[]? rented = null;
        scoped Span<TScope> scopes;
        if (_maxDepth == 0)
            scopes = default;
        else if (_maxDepth <= MaxStackScopeDepth)
            scopes = stackalloc TScope[_maxDepth];
        else
            scopes = rented = ArrayPool<TScope>.Shared.Rent(_maxDepth);
        var depth = 0;
        try
        {
            while (!visitor.StopVisiting && !reader.IsAtEnd)
            {
                switch (reader.Peek<RenderDataOpcode>())
                {
                    case RenderDataOpcode.DrawLine:
                    {
                        var p = reader.ReadPayload<DrawLinePayload>();
                        visitor.OnDrawLine(
                            (IPen?)_resources[p.ServerPen],
                            (IPen?)_resources[p.ClientPen],
                            p.P1, p.P2);
                        break;
                    }
                    case RenderDataOpcode.DrawRectangle:
                    {
                        var p = reader.ReadPayload<DrawRectanglePayload>();
                        var shadows = ReadBoxShadows(ref reader, p.BoxShadowCount);
                        visitor.OnDrawRectangle(
                            (IBrush?)_resources[p.ServerBrush],
                            (IPen?)_resources[p.ServerPen],
                            (IPen?)_resources[p.ClientPen],
                            p.Rect, shadows);
                        break;
                    }
                    case RenderDataOpcode.DrawEllipse:
                    {
                        var p = reader.ReadPayload<DrawEllipsePayload>();
                        visitor.OnDrawEllipse(
                            (IBrush?)_resources[p.ServerBrush],
                            (IPen?)_resources[p.ServerPen],
                            (IPen?)_resources[p.ClientPen],
                            p.Rect);
                        break;
                    }
                    case RenderDataOpcode.DrawGeometry:
                    {
                        var p = reader.ReadPayload<DrawGeometryPayload>();
                        visitor.OnDrawGeometry(
                            (IBrush?)_resources[p.ServerBrush],
                            (IPen?)_resources[p.ServerPen],
                            (IPen?)_resources[p.ClientPen],
                            (IGeometryImpl?)_resources[p.Geometry]);
                        break;
                    }
                    case RenderDataOpcode.DrawGlyphRun:
                    {
                        var p = reader.ReadPayload<DrawGlyphRunPayload>();
                        visitor.OnDrawGlyphRun(
                            (IBrush?)_resources[p.ServerBrush],
                            (IRef<IGlyphRunImpl>?)_resources[p.GlyphRun]);
                        break;
                    }
                    case RenderDataOpcode.DrawBitmap:
                    {
                        var p = reader.ReadPayload<DrawBitmapPayload>();
                        visitor.OnDrawBitmap(
                            (IRef<IBitmapImpl>?)_resources[p.Bitmap],
                            p.Opacity, p.SourceRect, p.DestRect);
                        break;
                    }
                    case RenderDataOpcode.DrawCustom:
                    {
                        var p = reader.ReadPayload<DrawCustomPayload>();
                        visitor.OnDrawCustom((ICustomDrawOperation?)_resources[p.Operation]);
                        break;
                    }
                    case RenderDataOpcode.PushClip:
                    {
                        var p = reader.ReadPayload<PushClipPayload>();
                        scopes[depth++] = visitor.OnPushClip(p.Clip);
                        break;
                    }
                    case RenderDataOpcode.PushGeometryClip:
                    {
                        var p = reader.ReadPayload<PushGeometryClipPayload>();
                        scopes[depth++] = visitor.OnPushGeometryClip((IGeometryImpl?)_resources[p.Geometry]);
                        break;
                    }
                    case RenderDataOpcode.PushOpacity:
                    {
                        var p = reader.ReadPayload<PushOpacityPayload>();
                        scopes[depth++] = visitor.OnPushOpacity(p.Opacity);
                        break;
                    }
                    case RenderDataOpcode.PushOpacityMask:
                    {
                        var p = reader.ReadPayload<PushOpacityMaskPayload>();
                        scopes[depth++] = visitor.OnPushOpacityMask(
                            (IBrush?)_resources[p.Brush], p.Bounds);
                        break;
                    }
                    case RenderDataOpcode.PushTransform:
                    {
                        var p = reader.ReadPayload<PushTransformPayload>();
                        scopes[depth++] = visitor.OnPushTransform(p.Matrix);
                        break;
                    }
                    case RenderDataOpcode.PushRenderOptions:
                    {
                        var p = reader.ReadPayload<PushRenderOptionsPayload>();
                        scopes[depth++] = visitor.OnPushRenderOptions(p.Options);
                        break;
                    }
                    case RenderDataOpcode.PushTextOptions:
                    {
                        var p = reader.ReadPayload<PushTextOptionsPayload>();
                        scopes[depth++] = visitor.OnPushTextOptions(p.Options);
                        break;
                    }
                    case RenderDataOpcode.PushEffect:
                    {
                        var p = reader.ReadPayload<PushEffectPayload>();
                        scopes[depth++] = visitor.OnPushEffect(
                            (IEffect?)_resources[p.Effect], p.Bounds);
                        break;
                    }
                    case RenderDataOpcode.Pop:
                    {
                        reader.Read<RenderDataOpcode>();
                        visitor.OnPop(in scopes[--depth]);
                        break;
                    }
                }
            }
        }
        finally
        {
            if (rented != null)
                ArrayPool<TScope>.Shared.Return(rented);
        }
    }

    private static BoxShadows ReadBoxShadows(ref RenderDataReader reader, int count)
    {
        if (count == 0)
            return default;

        var first = reader.Read<BoxShadow>();
        if (count == 1)
            return new BoxShadows(first);

        var rest = new BoxShadow[count - 1];
        for (var i = 0; i < rest.Length; i++)
            rest[i] = reader.Read<BoxShadow>();
        return new BoxShadows(first, rest);
    }
}
