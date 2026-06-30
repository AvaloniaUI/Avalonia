using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

    public ReadOnlySpan<byte> Take(int count)
    {
        var span = _buffer.Slice(_position, count);
        _position += count;
        return span;
    }

    public T Read<T>() where T : unmanaged
        => MemoryMarshal.Read<T>(Take(Unsafe.SizeOf<T>()));

    public T Peek<T>() where T : unmanaged
        => MemoryMarshal.Read<T>(_buffer.Slice(_position, Unsafe.SizeOf<T>()));

    public T ReadPayload<T>() where T : unmanaged, IRenderDataPayload<T>
    {
        var opcode = Read<RenderDataOpcode>();
        Debug.Assert(opcode == T.Opcode);
        return Read<T>();
    }
}
