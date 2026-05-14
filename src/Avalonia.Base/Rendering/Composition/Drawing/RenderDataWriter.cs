using System;
using System.Buffers;
using System.Buffers.Binary;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Avalonia.Rendering.Composition.Drawing;

internal struct RenderDataWriter : IDisposable
{
    private byte[]? _buffer;
    private int _length;

    public int Length => _length;

    public ReadOnlySpan<byte> Written => _buffer is null ? default : _buffer.AsSpan(0, _length);

    private Span<byte> Advance(int size)
    {
        var required = _length + size;
        if (_buffer is null)
            _buffer = ArrayPool<byte>.Shared.Rent(Math.Max(required, 256));
        else if (_buffer.Length < required)
        {
            var grown = ArrayPool<byte>.Shared.Rent(Math.Max(required, _buffer.Length * 2));
            Array.Copy(_buffer, grown, _length);
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = grown;
        }

        var span = _buffer.AsSpan(_length, size);
        _length += size;
        return span;
    }

    public Span<byte> Reserve(int count) => Advance(count);

    public void Rewind(int length) => _length = length;

    public void WriteByte(byte value) => Advance(1)[0] = value;

    public void WriteOpcode(RenderDataOpcode opcode) => WriteByte((byte)opcode);

    public void WriteInt32(int value) => BinaryPrimitives.WriteInt32LittleEndian(Advance(4), value);

    public void WriteUInt32(uint value) => BinaryPrimitives.WriteUInt32LittleEndian(Advance(4), value);

    public void WriteDouble(double value) => BinaryPrimitives.WriteDoubleLittleEndian(Advance(8), value);

    public void WriteBoolean(bool value) => WriteByte(value ? (byte)1 : (byte)0);

    public void WritePoint(Point value)
    {
        WriteDouble(value.X);
        WriteDouble(value.Y);
    }

    public void WriteVector(Vector value)
    {
        WriteDouble(value.X);
        WriteDouble(value.Y);
    }

    public void WriteRect(Rect value)
    {
        WriteDouble(value.X);
        WriteDouble(value.Y);
        WriteDouble(value.Width);
        WriteDouble(value.Height);
    }

    public void WriteRoundedRect(RoundedRect value)
    {
        WriteRect(value.Rect);
        WriteVector(value.RadiiTopLeft);
        WriteVector(value.RadiiTopRight);
        WriteVector(value.RadiiBottomRight);
        WriteVector(value.RadiiBottomLeft);
    }

    public void WriteMatrix(Matrix value)
    {
        WriteDouble(value.M11);
        WriteDouble(value.M12);
        WriteDouble(value.M13);
        WriteDouble(value.M21);
        WriteDouble(value.M22);
        WriteDouble(value.M23);
        WriteDouble(value.M31);
        WriteDouble(value.M32);
        WriteDouble(value.M33);
    }

    public void WriteColor(Color value) => WriteUInt32(value.ToUInt32());

    public void WriteBoxShadow(BoxShadow value)
    {
        WriteDouble(value.OffsetX);
        WriteDouble(value.OffsetY);
        WriteDouble(value.Blur);
        WriteDouble(value.Spread);
        WriteColor(value.Color);
        WriteBoolean(value.IsInset);
    }

    public void WriteRenderOptions(RenderOptions value)
    {
#pragma warning disable CS0618 // TextRenderingMode is obsolete but still carried for back-compat.
        WriteByte((byte)value.TextRenderingMode);
#pragma warning restore CS0618
        WriteByte((byte)value.BitmapInterpolationMode);
        WriteByte((byte)value.EdgeMode);
        WriteByte((byte)value.BitmapBlendingMode);
        WriteNullableBoolean(value.RequiresFullOpacityHandling);
    }

    public void WriteTextOptions(TextOptions value)
    {
        WriteByte((byte)value.TextRenderingMode);
        WriteByte((byte)value.TextHintingMode);
        WriteByte((byte)value.BaselinePixelAlignment);
    }

    private void WriteNullableBoolean(bool? value) =>
        WriteByte(value is null ? (byte)0 : value.Value ? (byte)2 : (byte)1);

    public void Dispose()
    {
        if (_buffer != null)
            ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = null;
        _length = 0;
    }
}
