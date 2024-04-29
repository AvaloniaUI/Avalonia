using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Platform.Interop;

namespace Avalonia.Vulkan.Interop;

internal unsafe class Utf8BufferArray : IDisposable
{
    private readonly List<Utf8Buffer> _buffers;
    private byte** _bufferArray;

    public Utf8BufferArray(IEnumerable<string> strings)
    {
        _buffers = strings.Select(x => new Utf8Buffer(x)).ToList();
        _bufferArray = (byte**)Marshal.AllocHGlobal(_buffers.Count * IntPtr.Size);
        for (var c = 0; c < _buffers.Count; c++)
            _bufferArray[c] = _buffers[c];
    }

    public static unsafe implicit operator byte**(Utf8BufferArray a) => a._bufferArray;

    public int Count => _buffers.Count;
    public uint UCount => (uint)Count;

    public void Dispose() => Dispose(true);
    void Dispose(bool disposing)
    {
        if (_bufferArray != null)
            Marshal.FreeHGlobal(new IntPtr(_bufferArray));
        _bufferArray = null;
        if (disposing)
        {
            foreach (var b in _buffers)
                b.Dispose();
            _buffers.Clear();
            GC.SuppressFinalize(this);
        }
    }

    ~Utf8BufferArray()
    {
        Dispose(false);
    }
}