using System;
using System.Buffers.Binary;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Avalonia.Rendering.Composition.Drawing;

internal ref struct RenderDataReader
{
    private readonly ReadOnlySpan<byte> _buffer;
    private int _position;

    public RenderDataReader(ReadOnlySpan<byte> buffer)
    {
        _buffer = buffer;
        _position = 0;
    }

    public int Position => _position;

    public bool IsAtEnd => _position >= _buffer.Length;

    private ReadOnlySpan<byte> Take(int size)
    {
        var span = _buffer.Slice(_position, size);
        _position += size;
        return span;
    }

    public byte ReadByte() => Take(1)[0];

    public RenderDataOpcode ReadOpcode() => (RenderDataOpcode)ReadByte();

    public int ReadInt32() => BinaryPrimitives.ReadInt32LittleEndian(Take(4));

    public uint ReadUInt32() => BinaryPrimitives.ReadUInt32LittleEndian(Take(4));

    public double ReadDouble() => BinaryPrimitives.ReadDoubleLittleEndian(Take(8));

    public bool ReadBoolean() => ReadByte() != 0;

    public Point ReadPoint() => new(ReadDouble(), ReadDouble());

    public Vector ReadVector() => new(ReadDouble(), ReadDouble());

    public Rect ReadRect() => new(ReadDouble(), ReadDouble(), ReadDouble(), ReadDouble());

    public RoundedRect ReadRoundedRect() =>
        new(ReadRect(), ReadVector(), ReadVector(), ReadVector(), ReadVector());

    public Matrix ReadMatrix() => new(
        ReadDouble(), ReadDouble(), ReadDouble(),
        ReadDouble(), ReadDouble(), ReadDouble(),
        ReadDouble(), ReadDouble(), ReadDouble());

    public Color ReadColor() => Color.FromUInt32(ReadUInt32());

    public BoxShadow ReadBoxShadow() => new()
    {
        OffsetX = ReadDouble(),
        OffsetY = ReadDouble(),
        Blur = ReadDouble(),
        Spread = ReadDouble(),
        Color = ReadColor(),
        IsInset = ReadBoolean()
    };

    public RenderOptions ReadRenderOptions() => new()
    {
#pragma warning disable CS0618 // TextRenderingMode is obsolete but still carried for back-compat.
        TextRenderingMode = (TextRenderingMode)ReadByte(),
#pragma warning restore CS0618
        BitmapInterpolationMode = (BitmapInterpolationMode)ReadByte(),
        EdgeMode = (EdgeMode)ReadByte(),
        BitmapBlendingMode = (BitmapBlendingMode)ReadByte(),
        RequiresFullOpacityHandling = ReadNullableBoolean()
    };

    public TextOptions ReadTextOptions() => new()
    {
        TextRenderingMode = (TextRenderingMode)ReadByte(),
        TextHintingMode = (TextHintingMode)ReadByte(),
        BaselinePixelAlignment = (BaselinePixelAlignment)ReadByte()
    };

    private bool? ReadNullableBoolean()
    {
        var value = ReadByte();
        return value == 0 ? null : value == 2;
    }
}
