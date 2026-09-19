using System;
using System.Runtime.InteropServices;
using NWayland;
using NWayland.Protocols.Wayland;

namespace Avalonia.Wayland.Embedding.Compositor;

/// <summary>libc shared-memory primitives for the wl_shm implementation. Compositor-thread only.</summary>
internal static class ShmNative
{
    public const int ProtRead = 0x01;
    public const int MapShared = 0x01;
    public static readonly IntPtr MapFailed = new(-1);

    // Linux file sealing: forbid a client from shrinking a pool fd out from under our buffer mappings.
    private const int FAddSeals = 1033; // F_LINUX_SPECIFIC_BASE (1024) + 9
    private const int FSealShrink = 0x0002;

    /// <summary>Best-effort: forbid shrinking <paramref name="fd"/>. A no-op when the fd isn't sealable.</summary>
    public static void TrySealShrink(int fd) => Fcntl(fd, FAddSeals, FSealShrink);

    [DllImport("libc", EntryPoint = "mmap", SetLastError = true)]
    public static extern IntPtr Mmap(IntPtr addr, nuint length, int prot, int flags, int fd, long offset);

    [DllImport("libc", EntryPoint = "munmap", SetLastError = true)]
    public static extern int Munmap(IntPtr addr, nuint length);

    [DllImport("libc", EntryPoint = "close", SetLastError = true)]
    public static extern int Close(int fd);

    [DllImport("libc", EntryPoint = "fcntl", SetLastError = true)]
    private static extern int Fcntl(int fd, int cmd, int arg);
}

/// <summary>A mapped buffer region: the page-aligned base to unmap, and the data pointer within it.</summary>
internal readonly struct ShmMapping
{
    public ShmMapping(IntPtr mapBase, IntPtr data, int mapLength)
    {
        MapBase = mapBase;
        Data = data;
        MapLength = mapLength;
    }

    public IntPtr MapBase { get; }
    public IntPtr Data { get; }
    public int MapLength { get; }
}

/// <summary>
/// A wl_shm_pool: just the shared-memory fd and its size. We deliberately do NOT mmap the pool — each buffer
/// maps its own region (<see cref="TryMapBuffer"/>), so a buffer's mapping keeps the shared memory alive even
/// after the pool's fd is closed. That matches the protocol: a client may destroy a pool while buffers made
/// from it are still in use. Compositor-thread only.
/// </summary>
internal sealed class ShmPoolState : IDisposable
{
    private int _fd;
    private int _size;
    private bool _disposed;

    public ShmPoolState(int fd, int size)
    {
        _fd = fd;
        _size = size;
        ShmNative.TrySealShrink(fd); // (1) forbid shrinking the fd under our future mappings
    }

    public int Size => _size;
    public bool IsDisposed => _disposed;

    /// <summary>
    /// Pools only grow (a client ftruncates up, then resizes); since each buffer maps independently there is
    /// nothing to remap here — we just track the new extent for bounds checks on future buffers.
    /// </summary>
    public void Resize(int newSize)
    {
        if (newSize > _size)
            _size = newSize;
    }

    /// <summary>
    /// (2) Map a buffer's region directly from the pool fd. The mapping holds its own reference to the shared
    /// memory, so it outlives the pool's fd being closed. Returns null when the region is out of bounds or
    /// the mmap fails — the caller then bails out (3).
    /// </summary>
    public ShmMapping? TryMapBuffer(int offset, int length)
    {
        if (_disposed || offset < 0 || length <= 0 || (long)offset + length > _size)
            return null;

        var pageSize = Environment.SystemPageSize;
        var alignedOffset = offset & ~(pageSize - 1);
        var delta = offset - alignedOffset;
        var mapLength = (long)delta + length; // long: delta can push a near-int.MaxValue length over the edge
        if (mapLength > int.MaxValue)
            return null;

        var mapBase = ShmNative.Mmap(IntPtr.Zero, (nuint)mapLength,
            ShmNative.ProtRead, ShmNative.MapShared, _fd, alignedOffset);
        if (mapBase == ShmNative.MapFailed)
            return null;

        return new ShmMapping(mapBase, mapBase + delta, (int)mapLength);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        if (_fd >= 0)
        {
            ShmNative.Close(_fd); // no munmap: live buffers own their own mappings
            _fd = -1;
        }
    }
}

/// <summary>A wl_buffer = a mapped rectangular region. Owns its mapping, independent of the pool fd.</summary>
internal sealed class ShmBufferState
{
    private ShmMapping? _mapping;
    private bool _released;

    public ShmBufferState(WlBuffer.Server resource, int width, int height, int stride,
        WlShm.FormatEnum format, ShmMapping mapping)
    {
        Resource = resource;
        Width = width;
        Height = height;
        Stride = stride;
        Format = format;
        _mapping = mapping;
    }

    public WlBuffer.Server Resource { get; }
    public int Width { get; }
    public int Height { get; }
    public int Stride { get; }
    public WlShm.FormatEnum Format { get; }

    public IntPtr GetData() => _mapping?.Data ?? IntPtr.Zero;

    /// <summary>
    /// We copy the SHM region into an Avalonia <c>Bitmap</c> once per commit (D6), so the buffer can be
    /// released back to the client immediately afterwards.
    /// </summary>
    public void Release()
    {
        if (_released)
            return;
        _released = true;
        try { Resource.Release(); } catch { /* client may already be gone */ }
    }

    public void Reattach() => _released = false;

    /// <summary>Unmap the region. Called on wl_buffer.destroy and on client teardown.</summary>
    public void DisposeMapping()
    {
        if (_mapping is { } m && m.MapBase != IntPtr.Zero)
            ShmNative.Munmap(m.MapBase, (nuint)m.MapLength);
        _mapping = null;
    }
}

internal sealed class ShmListener : WlShm.ServerListener
{
    private readonly ClientContext _client;
    public ShmListener(ClientContext client) => _client = client;

    protected override void CreatePool(WlShm.Server resource, NewId<WlShmPool.Server, WlShmPool.ServerListener> id, WaylandFd fd, int size)
    {
        var pool = new ShmPoolState(fd.Consume(), size);
        id.GetAndConsume(new ShmPoolListener(_client, pool));
    }

    protected override void Release(WlShm.Server resource) => resource.Dispose();
}

internal sealed class ShmPoolListener : WlShmPool.ServerListener
{
    private readonly ClientContext _client;
    private readonly ShmPoolState _pool;

    public ShmPoolListener(ClientContext client, ShmPoolState pool)
    {
        _client = client;
        _pool = pool;
    }

    protected override void CreateBuffer(WlShmPool.Server resource, NewId<WlBuffer.Server, WlBuffer.ServerListener> id,
        int offset, int width, int height, int stride, WlShm.FormatEnum format)
    {
        var listener = new BufferListener();
        var bufferResource = id.GetAndConsume(listener); // consume the new_id regardless, to keep the object map consistent

        var byteLength = (long)height * stride;
        var mapping = width > 0 && height > 0 && stride > 0 && byteLength <= int.MaxValue
            ? _pool.TryMapBuffer(offset, (int)byteLength)
            : null;

        if (mapping is null)
        {
            // (3) We can't back this buffer with memory — bail rather than hand out a buffer that reads
            // garbage. PostError is connection-fatal (NWayland queues wl_display.error and disconnects the
            // client); the following Dispose is then a redundant-but-safe no-op.
            bufferResource.PostError(WlDisplay.ErrorEnum.NoMemory,
                "wl_shm_pool.create_buffer: region is out of bounds or could not be mapped");
            bufferResource.Dispose();
            return;
        }

        var state = new ShmBufferState(bufferResource, width, height, stride, format, mapping.Value);
        listener.Init(_client, state);
    }

    protected override void Resize(WlShmPool.Server resource, int size) => _pool.Resize(size);

    protected override void Destroy(WlShmPool.Server resource)
    {
        _pool.Dispose();
        resource.Dispose();
    }
}

internal sealed class BufferListener : WlBuffer.ServerListener
{
    private ClientContext _client = null!;
    private ShmBufferState _state = null!;

    public void Init(ClientContext client, ShmBufferState state)
    {
        _client = client;
        _state = state;
        _client.RegisterBuffer(state);
    }

    protected override void Destroy(WlBuffer.Server resource)
    {
        _client.UnregisterBuffer(_state);
        _state.DisposeMapping();
        resource.Dispose();
    }
}
