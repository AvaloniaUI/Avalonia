using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

    public void Write<T>(T value) where T : unmanaged
        => MemoryMarshal.Write(Advance(Unsafe.SizeOf<T>()), in value);

    public void WriteOpcode(RenderDataOpcode opcode) => Write(opcode);

    public void WritePayload<T>(T payload) where T : unmanaged, IRenderDataPayload<T>
    {
        Write(T.Opcode);
        Write(payload);
    }

    public void Dispose()
    {
        if (_buffer != null)
            ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = null;
        _length = 0;
    }
}
