using System;
using System.Runtime.InteropServices;

namespace Avalonia.Wayland.Embedding.Tests;

internal static class LibC
{
    private const string Lib = "libc";

    [DllImport(Lib, EntryPoint = "memfd_create", SetLastError = true)]
    public static extern int MemfdCreate(string name, uint flags);

    [DllImport(Lib, EntryPoint = "ftruncate", SetLastError = true)]
    public static extern int Ftruncate(int fd, long length);

    [DllImport(Lib, EntryPoint = "mmap", SetLastError = true)]
    public static extern IntPtr Mmap(IntPtr addr, nuint length, int prot, int flags, int fd, long offset);

    [DllImport(Lib, EntryPoint = "munmap", SetLastError = true)]
    public static extern int Munmap(IntPtr addr, nuint length);

    [DllImport(Lib, EntryPoint = "close", SetLastError = true)]
    public static extern int Close(int fd);

    public const uint MFD_CLOEXEC = 0x1;
    public const uint MFD_ALLOW_SEALING = 0x2;
    public const int PROT_READ = 0x1;
    public const int PROT_WRITE = 0x2;
    public const int MAP_SHARED = 0x1;
}

/// <summary>
/// An anonymous shared-memory region (a <c>memfd</c>) the test client hands to the compositor as a
/// <c>wl_shm</c> pool fd. Owns the fd + mapping; the fd stays open for the region's lifetime so the
/// compositor can re-read it across multiple committed buffers.
/// </summary>
internal sealed unsafe class SharedMemoryBuffer : IDisposable
{
    public int Fd { get; }
    public int Size { get; }
    public IntPtr Data { get; }

    private SharedMemoryBuffer(int fd, int size, IntPtr data)
    {
        Fd = fd;
        Size = size;
        Data = data;
    }

    public static SharedMemoryBuffer Create(int size)
    {
        // ALLOW_SEALING so the compositor can add F_SEAL_SHRINK on pool creation (as a real client's fd allows).
        var fd = LibC.MemfdCreate("wlembed-test", LibC.MFD_CLOEXEC | LibC.MFD_ALLOW_SEALING);
        if (fd < 0)
            throw new InvalidOperationException($"memfd_create failed: errno {Marshal.GetLastPInvokeError()}");
        if (LibC.Ftruncate(fd, size) != 0)
        {
            LibC.Close(fd);
            throw new InvalidOperationException($"ftruncate failed: errno {Marshal.GetLastPInvokeError()}");
        }

        var data = LibC.Mmap(IntPtr.Zero, (nuint)size, LibC.PROT_READ | LibC.PROT_WRITE, LibC.MAP_SHARED, fd, 0);
        if (data == IntPtr.Zero || data == new IntPtr(-1))
        {
            LibC.Close(fd);
            throw new InvalidOperationException($"mmap failed: errno {Marshal.GetLastPInvokeError()}");
        }

        return new SharedMemoryBuffer(fd, size, data);
    }

    /// <summary>Fill the whole region with a single 32-bit pixel value (native byte order).</summary>
    public void Fill(uint pixel)
    {
        var p = (uint*)Data;
        for (var i = 0; i < Size / 4; i++)
            p[i] = pixel;
    }

    /// <summary>Fill the byte range [byteOffset, byteOffset+byteLength) with a 32-bit pixel value.</summary>
    public void FillRange(int byteOffset, int byteLength, uint pixel)
    {
        var p = (uint*)(Data + byteOffset);
        for (var i = 0; i < byteLength / 4; i++)
            p[i] = pixel;
    }

    /// <summary>Read one pixel at a byte offset (native byte order).</summary>
    public uint PixelAt(int byteOffset) => *(uint*)(Data + byteOffset);

    public void Dispose()
    {
        if (Data != IntPtr.Zero)
            LibC.Munmap(Data, (nuint)Size);
        if (Fd >= 0)
            LibC.Close(Fd);
    }
}
