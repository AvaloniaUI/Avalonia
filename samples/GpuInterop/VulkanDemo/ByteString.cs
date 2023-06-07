using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace GpuInterop.VulkanDemo;

unsafe class ByteString : IDisposable
{
    public IntPtr Pointer { get; }

    public ByteString(string s)
    {
        Pointer = Marshal.StringToHGlobalAnsi(s);
    }

    public void Dispose()
    {
        Marshal.FreeHGlobal(Pointer);
    }

    public static implicit operator byte*(ByteString h) => (byte*)h.Pointer;
}
    
unsafe class ByteStringList : IDisposable
{
    private List<ByteString> _inner;
    private byte** _ptr;

    public ByteStringList(IEnumerable<string> items)
    {
        _inner = items.Select(x => new ByteString(x)).ToList();
        _ptr = (byte**)Marshal.AllocHGlobal(IntPtr.Size * _inner.Count + 1);
        for (var c = 0; c < _inner.Count; c++)
            _ptr[c] = (byte*)_inner[c].Pointer;
    }

    public int Count => _inner.Count;
    public uint UCount => (uint)_inner.Count;

    public void Dispose()
    {
        Marshal.FreeHGlobal(new IntPtr(_ptr));
    }

    public static implicit operator byte**(ByteStringList h) => h._ptr;
}
