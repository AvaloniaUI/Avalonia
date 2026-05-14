using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Threading;
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

    private static readonly ThreadSafeObjectPool<Stack<BoundsScope>> s_boundsScopePool = new();

    public Rect? CalculateBounds()
    {
        var reader = new RenderDataReader(_writer.Written);
        var scopes = s_boundsScopePool.Get();
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
                        scopes.Push(new BoundsScope { SavedBounds = current });
                        current = null;
                        break;
                    }
                    case RenderDataOpcode.PushGeometryClip:
                    {
                        reader.ReadInt32();
                        scopes.Push(new BoundsScope { SavedBounds = current });
                        current = null;
                        break;
                    }
                    case RenderDataOpcode.PushOpacity:
                    {
                        reader.ReadDouble();
                        scopes.Push(new BoundsScope { SavedBounds = current });
                        current = null;
                        break;
                    }
                    case RenderDataOpcode.PushOpacityMask:
                    {
                        reader.ReadInt32();
                        reader.ReadRect();
                        scopes.Push(new BoundsScope { SavedBounds = current });
                        current = null;
                        break;
                    }
                    case RenderDataOpcode.PushTransform:
                    {
                        var matrix = reader.ReadMatrix();
                        scopes.Push(new BoundsScope { SavedBounds = current, IsTransform = true, Matrix = matrix });
                        current = null;
                        break;
                    }
                    case RenderDataOpcode.PushRenderOptions:
                    {
                        reader.ReadRenderOptions();
                        scopes.Push(new BoundsScope { SavedBounds = current });
                        current = null;
                        break;
                    }
                    case RenderDataOpcode.PushTextOptions:
                    {
                        reader.ReadTextOptions();
                        scopes.Push(new BoundsScope { SavedBounds = current });
                        current = null;
                        break;
                    }
                    case RenderDataOpcode.Pop:
                    {
                        var scope = scopes.Pop();
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
            scopes.Clear();
            s_boundsScopePool.ReturnAndSetNull(ref scopes);
        }
    }
}
