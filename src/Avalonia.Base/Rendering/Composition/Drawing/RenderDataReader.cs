using System;
using System.Runtime.InteropServices;
using Avalonia.Media;

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

    private T ReadBlittable<T>() where T : unmanaged
    {
        T value = default;
        var bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1));
        Take(bytes.Length).CopyTo(bytes);
        return value;
    }

    public byte ReadByte() => Take(1)[0];

    public RenderDataOpcode ReadOpcode() => (RenderDataOpcode)ReadByte();

    public int ReadInt32() => ReadBlittable<int>();

    public uint ReadUInt32() => ReadBlittable<uint>();

    public double ReadDouble() => ReadBlittable<double>();

    public bool ReadBoolean() => ReadBlittable<bool>();

    public Point ReadPoint() => ReadBlittable<Point>();

    public Vector ReadVector() => ReadBlittable<Vector>();

    public Rect ReadRect() => ReadBlittable<Rect>();

    public RoundedRect ReadRoundedRect() => ReadBlittable<RoundedRect>();

    public Matrix ReadMatrix() => ReadBlittable<Matrix>();

    public Color ReadColor() => ReadBlittable<Color>();

    public BoxShadow ReadBoxShadow() => ReadBlittable<BoxShadow>();

    public RenderOptions ReadRenderOptions() => ReadBlittable<RenderOptions>();

    public TextOptions ReadTextOptions() => ReadBlittable<TextOptions>();
}
