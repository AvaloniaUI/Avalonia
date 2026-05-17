using System;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Drawing;

internal partial class RenderDataStream : IDisposable
{
    private RenderDataWriter _writer;
    private RenderDataResources _resources;
    private int _depth;
    private int _maxDepth;

    public ReadOnlySpan<byte> Opcodes => _writer.Written;

    public int OpcodeLength => _writer.Length;

    public int Depth => _depth;

    public int ResourceCount => _resources.Count;

    public object? GetResource(int handle) => _resources[handle];

    public void Rewind(int length, int depth)
    {
        _writer.Rewind(length);
        _depth = depth;
    }

    private void EnterScope()
    {
        _depth++;
        if (_depth > _maxDepth)
            _maxDepth = _depth;
    }

    public void DisposeResources()
    {
        for (var i = 0; i < _resources.Count; i++)
        {
            switch (_resources[i])
            {
                case IRef<IBitmapImpl> bitmap:
                    bitmap.Dispose();
                    break;
                case IRef<IGlyphRunImpl> glyphRun:
                    glyphRun.Dispose();
                    break;
                case ICustomDrawOperation operation:
                    operation.Dispose();
                    break;
            }
        }
    }

    public void DrawLine(IPen? serverPen, IPen? clientPen, Point p1, Point p2)
    {
        _writer.WritePayload(new DrawLinePayload
        {
            ServerPen = _resources.Intern(serverPen),
            ClientPen = _resources.Intern(clientPen),
            P1 = p1,
            P2 = p2
        });
    }

    public void DrawRectangle(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, RoundedRect rect,
        BoxShadows boxShadows)
    {
        _writer.WritePayload(new DrawRectanglePayload
        {
            ServerBrush = _resources.Intern(serverBrush),
            ServerPen = _resources.Intern(serverPen),
            ClientPen = _resources.Intern(clientPen),
            Rect = rect,
            BoxShadowCount = boxShadows.Count
        });
        for (var i = 0; i < boxShadows.Count; i++)
            _writer.Write(boxShadows[i]);
    }

    public void DrawEllipse(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, Rect rect)
    {
        _writer.WritePayload(new DrawEllipsePayload
        {
            ServerBrush = _resources.Intern(serverBrush),
            ServerPen = _resources.Intern(serverPen),
            ClientPen = _resources.Intern(clientPen),
            Rect = rect
        });
    }

    public void DrawGeometry(IBrush? serverBrush, IPen? serverPen, IPen? clientPen, IGeometryImpl? geometry)
    {
        _writer.WritePayload(new DrawGeometryPayload
        {
            ServerBrush = _resources.Intern(serverBrush),
            ServerPen = _resources.Intern(serverPen),
            ClientPen = _resources.Intern(clientPen),
            Geometry = _resources.Intern(geometry)
        });
    }

    public void DrawGlyphRun(IBrush? serverBrush, IRef<IGlyphRunImpl>? glyphRun)
    {
        _writer.WritePayload(new DrawGlyphRunPayload
        {
            ServerBrush = _resources.Intern(serverBrush),
            GlyphRun = _resources.Intern(glyphRun)
        });
    }

    public void DrawBitmap(IRef<IBitmapImpl>? bitmap, double opacity, Rect sourceRect, Rect destRect)
    {
        _writer.WritePayload(new DrawBitmapPayload
        {
            Bitmap = _resources.Intern(bitmap),
            Opacity = opacity,
            SourceRect = sourceRect,
            DestRect = destRect
        });
    }

    public void DrawCustom(ICustomDrawOperation? operation)
    {
        _writer.WritePayload(new DrawCustomPayload
        {
            Operation = _resources.Intern(operation)
        });
    }

    public void PushClip(RoundedRect clip)
    {
        _writer.WritePayload(new PushClipPayload { Clip = clip });
        EnterScope();
    }

    public void PushGeometryClip(IGeometryImpl? geometry)
    {
        _writer.WritePayload(new PushGeometryClipPayload { Geometry = _resources.Intern(geometry) });
        EnterScope();
    }

    public void PushOpacity(double opacity)
    {
        _writer.WritePayload(new PushOpacityPayload { Opacity = opacity });
        EnterScope();
    }

    public void PushOpacityMask(IBrush? serverBrush, Rect bounds)
    {
        _writer.WritePayload(new PushOpacityMaskPayload
        {
            Brush = _resources.Intern(serverBrush),
            Bounds = bounds
        });
        EnterScope();
    }

    public void PushTransform(Matrix matrix)
    {
        _writer.WritePayload(new PushTransformPayload { Matrix = matrix });
        EnterScope();
    }

    public void PushRenderOptions(RenderOptions renderOptions)
    {
        _writer.WritePayload(new PushRenderOptionsPayload { Options = renderOptions });
        EnterScope();
    }

    public void PushTextOptions(TextOptions textOptions)
    {
        _writer.WritePayload(new PushTextOptionsPayload { Options = textOptions });
        EnterScope();
    }

    public void Pop()
    {
        _writer.WriteOpcode(RenderDataOpcode.Pop);
        _depth--;
    }

    public void SerializeTo(BatchStreamWriter writer)
    {
        var opcodes = _writer.Written;
        writer.Write(_maxDepth);
        writer.Write(_resources.Count);
        for (var i = 0; i < _resources.Count; i++)
            writer.WriteObject(_resources[i]);
        writer.Write(opcodes.Length);
        writer.Write(opcodes);
    }

    public void DeserializeFrom(BatchStreamReader reader)
    {
        _maxDepth = reader.Read<int>();

        var resourceCount = reader.Read<int>();
        for (var i = 0; i < resourceCount; i++)
            _resources.Add(reader.ReadObject());

        var byteCount = reader.Read<int>();
        if (byteCount > 0)
            reader.Read(_writer.Reserve(byteCount));
    }

    public void Dispose()
    {
        _writer.Dispose();
        _resources.Dispose();
    }
}
