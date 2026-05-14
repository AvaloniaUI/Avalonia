using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Drawing;

internal partial class RenderDataStream : IDisposable
{
    private RenderDataWriter _writer;
    private RenderDataResources _resources;

    private struct ReplayScope
    {
        public RenderDataOpcode Kind;
        public bool Active;
        public Matrix SavedTransform;
    }

    private static readonly ThreadSafeObjectPool<Stack<ReplayScope>> s_scopePool = new();

    public ReadOnlySpan<byte> Opcodes => _writer.Written;

    public void DrawLine(IPen? serverPen, IPen? clientPen, Point p1, Point p2)
    {
        _writer.WriteOpcode(RenderDataOpcode.DrawLine);
        _writer.WriteInt32(_resources.Intern(serverPen));
        _writer.WriteInt32(_resources.Intern(clientPen));
        _writer.WritePoint(p1);
        _writer.WritePoint(p2);
    }

    public void DrawRectangle(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, RoundedRect rect,
        BoxShadows boxShadows)
    {
        _writer.WriteOpcode(RenderDataOpcode.DrawRectangle);
        _writer.WriteInt32(_resources.Intern(serverBrush));
        _writer.WriteInt32(_resources.Intern(serverPen));
        _writer.WriteInt32(_resources.Intern(clientPen));
        _writer.WriteRoundedRect(rect);
        _writer.WriteInt32(boxShadows.Count);
        for (var i = 0; i < boxShadows.Count; i++)
            _writer.WriteBoxShadow(boxShadows[i]);
    }

    public void DrawEllipse(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, Rect rect)
    {
        _writer.WriteOpcode(RenderDataOpcode.DrawEllipse);
        _writer.WriteInt32(_resources.Intern(serverBrush));
        _writer.WriteInt32(_resources.Intern(serverPen));
        _writer.WriteInt32(_resources.Intern(clientPen));
        _writer.WriteRect(rect);
    }

    public void DrawGeometry(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, IGeometryImpl? geometry)
    {
        _writer.WriteOpcode(RenderDataOpcode.DrawGeometry);
        _writer.WriteInt32(_resources.Intern(serverBrush));
        _writer.WriteInt32(_resources.Intern(serverPen));
        _writer.WriteInt32(_resources.Intern(clientPen));
        _writer.WriteInt32(_resources.Intern(geometry));
    }

    public void DrawGlyphRun(IBrush? serverBrush, IRef<IGlyphRunImpl>? glyphRun)
    {
        _writer.WriteOpcode(RenderDataOpcode.DrawGlyphRun);
        _writer.WriteInt32(_resources.Intern(serverBrush));
        _writer.WriteInt32(_resources.Intern(glyphRun));
    }

    public void DrawBitmap(IRef<IBitmapImpl>? bitmap, double opacity, Rect sourceRect, Rect destRect)
    {
        _writer.WriteOpcode(RenderDataOpcode.DrawBitmap);
        _writer.WriteInt32(_resources.Intern(bitmap));
        _writer.WriteDouble(opacity);
        _writer.WriteRect(sourceRect);
        _writer.WriteRect(destRect);
    }

    public void DrawCustom(ICustomDrawOperation? operation)
    {
        _writer.WriteOpcode(RenderDataOpcode.DrawCustom);
        _writer.WriteInt32(_resources.Intern(operation));
    }

    public void PushClip(RoundedRect clip)
    {
        _writer.WriteOpcode(RenderDataOpcode.PushClip);
        _writer.WriteRoundedRect(clip);
    }

    public void PushGeometryClip(IGeometryImpl? geometry)
    {
        _writer.WriteOpcode(RenderDataOpcode.PushGeometryClip);
        _writer.WriteInt32(_resources.Intern(geometry));
    }

    public void PushOpacity(double opacity)
    {
        _writer.WriteOpcode(RenderDataOpcode.PushOpacity);
        _writer.WriteDouble(opacity);
    }

    public void PushOpacityMask(IBrush? serverBrush, Rect bounds)
    {
        _writer.WriteOpcode(RenderDataOpcode.PushOpacityMask);
        _writer.WriteInt32(_resources.Intern(serverBrush));
        _writer.WriteRect(bounds);
    }

    public void PushTransform(Matrix matrix)
    {
        _writer.WriteOpcode(RenderDataOpcode.PushTransform);
        _writer.WriteMatrix(matrix);
    }

    public void PushRenderOptions(RenderOptions renderOptions)
    {
        _writer.WriteOpcode(RenderDataOpcode.PushRenderOptions);
        _writer.WriteRenderOptions(renderOptions);
    }

    public void PushTextOptions(TextOptions textOptions)
    {
        _writer.WriteOpcode(RenderDataOpcode.PushTextOptions);
        _writer.WriteTextOptions(textOptions);
    }

    public void Pop() => _writer.WriteOpcode(RenderDataOpcode.Pop);

    public void Replay(IDrawingContextImpl context)
    {
        var reader = new RenderDataReader(_writer.Written);
        var scopes = s_scopePool.Get();
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
                        context.DrawLine(serverPen, reader.ReadPoint(), reader.ReadPoint());
                        break;
                    }
                    case RenderDataOpcode.DrawRectangle:
                    {
                        var brush = (IBrush?)_resources[reader.ReadInt32()];
                        var pen = (IPen?)_resources[reader.ReadInt32()];
                        reader.ReadInt32();
                        var rect = reader.ReadRoundedRect();
                        context.DrawRectangle(brush, pen, rect, ReadBoxShadows(ref reader));
                        break;
                    }
                    case RenderDataOpcode.DrawEllipse:
                    {
                        var brush = (IBrush?)_resources[reader.ReadInt32()];
                        var pen = (IPen?)_resources[reader.ReadInt32()];
                        reader.ReadInt32();
                        context.DrawEllipse(brush, pen, reader.ReadRect());
                        break;
                    }
                    case RenderDataOpcode.DrawGeometry:
                    {
                        var brush = (IBrush?)_resources[reader.ReadInt32()];
                        var pen = (IPen?)_resources[reader.ReadInt32()];
                        reader.ReadInt32();
                        var geometry = (IGeometryImpl?)_resources[reader.ReadInt32()];
                        if (geometry != null)
                            context.DrawGeometry(brush, pen, geometry);
                        break;
                    }
                    case RenderDataOpcode.DrawGlyphRun:
                    {
                        var brush = (IBrush?)_resources[reader.ReadInt32()];
                        var glyphRun = (IRef<IGlyphRunImpl>?)_resources[reader.ReadInt32()];
                        if (glyphRun != null)
                            context.DrawGlyphRun(brush, glyphRun.Item);
                        break;
                    }
                    case RenderDataOpcode.DrawBitmap:
                    {
                        var bitmap = (IRef<IBitmapImpl>?)_resources[reader.ReadInt32()];
                        var opacity = reader.ReadDouble();
                        var sourceRect = reader.ReadRect();
                        var destRect = reader.ReadRect();
                        if (bitmap != null)
                            context.DrawBitmap(bitmap.Item, opacity, sourceRect, destRect);
                        break;
                    }
                    case RenderDataOpcode.DrawCustom:
                    {
                        var operation = (ICustomDrawOperation?)_resources[reader.ReadInt32()];
                        operation?.Render(new ImmediateDrawingContext(context, false));
                        break;
                    }
                    case RenderDataOpcode.PushClip:
                    {
                        var clip = reader.ReadRoundedRect();
                        context.PushClip(clip);
                        scopes.Push(new ReplayScope { Kind = RenderDataOpcode.PushClip, Active = true });
                        break;
                    }
                    case RenderDataOpcode.PushGeometryClip:
                    {
                        var geometry = (IGeometryImpl?)_resources[reader.ReadInt32()];
                        if (geometry != null)
                            context.PushGeometryClip(geometry);
                        scopes.Push(new ReplayScope
                            { Kind = RenderDataOpcode.PushGeometryClip, Active = geometry != null });
                        break;
                    }
                    case RenderDataOpcode.PushOpacity:
                    {
                        var opacity = reader.ReadDouble();
                        if (opacity != 1)
                            context.PushOpacity(opacity, null);
                        scopes.Push(new ReplayScope
                            { Kind = RenderDataOpcode.PushOpacity, Active = opacity != 1 });
                        break;
                    }
                    case RenderDataOpcode.PushOpacityMask:
                    {
                        var brush = (IBrush?)_resources[reader.ReadInt32()];
                        var bounds = reader.ReadRect();
                        if (brush != null)
                            context.PushOpacityMask(brush, bounds);
                        scopes.Push(new ReplayScope
                            { Kind = RenderDataOpcode.PushOpacityMask, Active = brush != null });
                        break;
                    }
                    case RenderDataOpcode.PushTransform:
                    {
                        var matrix = reader.ReadMatrix();
                        var saved = context.Transform;
                        context.Transform = matrix * saved;
                        scopes.Push(new ReplayScope
                            { Kind = RenderDataOpcode.PushTransform, Active = true, SavedTransform = saved });
                        break;
                    }
                    case RenderDataOpcode.PushRenderOptions:
                    {
                        context.PushRenderOptions(reader.ReadRenderOptions());
                        scopes.Push(new ReplayScope { Kind = RenderDataOpcode.PushRenderOptions, Active = true });
                        break;
                    }
                    case RenderDataOpcode.PushTextOptions:
                    {
                        context.PushTextOptions(reader.ReadTextOptions());
                        scopes.Push(new ReplayScope { Kind = RenderDataOpcode.PushTextOptions, Active = true });
                        break;
                    }
                    case RenderDataOpcode.Pop:
                    {
                        var scope = scopes.Pop();
                        if (scope.Active)
                            PopScope(context, scope);
                        break;
                    }
                }
            }
        }
        finally
        {
            scopes.Clear();
            s_scopePool.ReturnAndSetNull(ref scopes);
        }
    }

    private static void PopScope(IDrawingContextImpl context, ReplayScope scope)
    {
        switch (scope.Kind)
        {
            case RenderDataOpcode.PushClip:
                context.PopClip();
                break;
            case RenderDataOpcode.PushGeometryClip:
                context.PopGeometryClip();
                break;
            case RenderDataOpcode.PushOpacity:
                context.PopOpacity();
                break;
            case RenderDataOpcode.PushOpacityMask:
                context.PopOpacityMask();
                break;
            case RenderDataOpcode.PushTransform:
                context.Transform = scope.SavedTransform;
                break;
            case RenderDataOpcode.PushRenderOptions:
                context.PopRenderOptions();
                break;
            case RenderDataOpcode.PushTextOptions:
                context.PopTextOptions();
                break;
        }
    }

    private static BoxShadows ReadBoxShadows(ref RenderDataReader reader)
    {
        var count = reader.ReadInt32();
        if (count == 0)
            return default;

        var first = reader.ReadBoxShadow();
        if (count == 1)
            return new BoxShadows(first);

        var rest = new BoxShadow[count - 1];
        for (var i = 0; i < rest.Length; i++)
            rest[i] = reader.ReadBoxShadow();
        return new BoxShadows(first, rest);
    }

    public void Dispose()
    {
        _writer.Dispose();
        _resources.Dispose();
    }
}
