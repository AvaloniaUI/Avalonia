using System;
using System.Buffers;
using System.Runtime.InteropServices;
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

    private void WriteBlittable<T>(T value) where T : unmanaged
    {
        var bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1));
        bytes.CopyTo(Advance(bytes.Length));
    }

    public Span<byte> Reserve(int count) => Advance(count);

    public void Rewind(int length) => _length = length;

    public void WriteByte(byte value) => Advance(1)[0] = value;

    public void WriteOpcode(RenderDataOpcode opcode) => WriteByte((byte)opcode);

    public void WriteInt32(int value) => WriteBlittable(value);

    public void WriteUInt32(uint value) => WriteBlittable(value);

    public void WriteDouble(double value) => WriteBlittable(value);

    public void WriteBoolean(bool value) => WriteBlittable(value);

    public void WritePoint(Point value) => WriteBlittable(value);

    public void WriteVector(Vector value) => WriteBlittable(value);

    public void WriteRect(Rect value) => WriteBlittable(value);

    public void WriteRoundedRect(RoundedRect value) => WriteBlittable(value);

    public void WriteMatrix(Matrix value) => WriteBlittable(value);

    public void WriteColor(Color value) => WriteBlittable(value);

    public void WriteBoxShadow(BoxShadow value) => WriteBlittable(value);

    public void WriteRenderOptions(RenderOptions value) => WriteBlittable(value);

    public void WriteTextOptions(TextOptions value) => WriteBlittable(value);

    public void Dispose()
    {
        if (_buffer != null)
            ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = null;
        _length = 0;
    }
}
