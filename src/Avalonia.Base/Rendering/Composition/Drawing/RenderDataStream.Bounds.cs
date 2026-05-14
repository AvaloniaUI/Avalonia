using System;
using System.Buffers;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Drawing;

internal partial class RenderDataStream
{
    private struct BoundsScope
    {
        public Rect? SavedBounds;
        public bool IsTransform;
        public Matrix Matrix;
    }

    public Rect? CalculateBounds()
    {
        var reader = new RenderDataReader(_writer.Written);
        BoundsScope[]? rented = null;
        scoped Span<BoundsScope> scopes;
        if (_maxDepth <= MaxStackScopeDepth)
            scopes = stackalloc BoundsScope[_maxDepth];
        else
            scopes = rented = ArrayPool<BoundsScope>.Shared.Rent(_maxDepth);
        var depth = 0;
        Rect? current = null;
        try
        {
            while (!reader.IsAtEnd)
            {
                switch (reader.ReadOpcode())
                {
                    case RenderDataOpcode.DrawLine:
                    {
                        var serverPen = (IPen?)_resources[reader.ReadInt32()];
                        reader.ReadInt32();
                        var p1 = reader.ReadPoint();
                        var p2 = reader.ReadPoint();
                        if (serverPen != null)
                            current = Rect.Union(current, LineBoundsHelper.CalculateBounds(p1, p2, serverPen));
                        break;
                    }
                    case RenderDataOpcode.DrawRectangle:
                    {
                        reader.ReadInt32();
                        var serverPen = (IPen?)_resources[reader.ReadInt32()];
                        reader.ReadInt32();
                        var rect = reader.ReadRoundedRect();
                        var boxShadows = ReadBoxShadows(ref reader);
                        var bounds = boxShadows.TransformBounds(rect.Rect)
                            .Inflate((serverPen?.Thickness ?? 0) / 2);
                        current = Rect.Union(current, bounds);
                        break;
                    }
                    case RenderDataOpcode.DrawEllipse:
                    {
                        reader.ReadInt32();
                        var serverPen = (IPen?)_resources[reader.ReadInt32()];
                        reader.ReadInt32();
                        var rect = reader.ReadRect();
                        current = Rect.Union(current, rect.Inflate(serverPen?.Thickness ?? 0));
                        break;
                    }
                    case RenderDataOpcode.DrawGeometry:
                    {
                        reader.ReadInt32();
                        var serverPen = (IPen?)_resources[reader.ReadInt32()];
                        reader.ReadInt32();
                        var geometry = (IGeometryImpl?)_resources[reader.ReadInt32()];
                        current = Rect.Union(current, geometry?.GetRenderBounds(serverPen) ?? default);
                        break;
                    }
                    case RenderDataOpcode.DrawGlyphRun:
                    {
                        reader.ReadInt32();
                        var glyphRun = (IRef<IGlyphRunImpl>?)_resources[reader.ReadInt32()];
                        current = Rect.Union(current, glyphRun?.Item?.Bounds ?? default);
                        break;
                    }
                    case RenderDataOpcode.DrawBitmap:
                    {
                        reader.ReadInt32();
                        reader.ReadDouble();
                        reader.ReadRect();
                        var destRect = reader.ReadRect();
                        current = Rect.Union(current, destRect);
                        break;
                    }
                    case RenderDataOpcode.DrawCustom:
                    {
                        var operation = (ICustomDrawOperation?)_resources[reader.ReadInt32()];
                        current = Rect.Union(current, operation?.Bounds);
                        break;
                    }
                    case RenderDataOpcode.PushClip:
                    {
                        reader.ReadRoundedRect();
                        scopes[depth++] = new BoundsScope { SavedBounds = current };
                        current = null;
                        break;
                    }
                    case RenderDataOpcode.PushGeometryClip:
                    {
                        reader.ReadInt32();
                        scopes[depth++] = new BoundsScope { SavedBounds = current };
                        current = null;
                        break;
                    }
                    case RenderDataOpcode.PushOpacity:
                    {
                        reader.ReadDouble();
                        scopes[depth++] = new BoundsScope { SavedBounds = current };
                        current = null;
                        break;
                    }
                    case RenderDataOpcode.PushOpacityMask:
                    {
                        reader.ReadInt32();
                        reader.ReadRect();
                        scopes[depth++] = new BoundsScope { SavedBounds = current };
                        current = null;
                        break;
                    }
                    case RenderDataOpcode.PushTransform:
                    {
                        var matrix = reader.ReadMatrix();
                        scopes[depth++] = new BoundsScope { SavedBounds = current, IsTransform = true, Matrix = matrix };
                        current = null;
                        break;
                    }
                    case RenderDataOpcode.PushRenderOptions:
                    {
                        reader.ReadRenderOptions();
                        scopes[depth++] = new BoundsScope { SavedBounds = current };
                        current = null;
                        break;
                    }
                    case RenderDataOpcode.PushTextOptions:
                    {
                        reader.ReadTextOptions();
                        scopes[depth++] = new BoundsScope { SavedBounds = current };
                        current = null;
                        break;
                    }
                    case RenderDataOpcode.Pop:
                    {
                        var scope = scopes[--depth];
                        var childUnion = current;
                        if (scope.IsTransform)
                            childUnion = childUnion?.TransformToAABB(scope.Matrix);
                        current = Rect.Union(scope.SavedBounds, childUnion);
                        break;
                    }
                }
            }

            return current;
        }
        finally
        {
            if (rented != null)
                ArrayPool<BoundsScope>.Shared.Return(rented);
        }
    }
}
