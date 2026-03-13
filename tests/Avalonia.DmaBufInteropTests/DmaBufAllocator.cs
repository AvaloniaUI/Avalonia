using System;
using System.IO;
using System.Runtime.InteropServices;
using static Avalonia.DmaBufInteropTests.NativeInterop;

namespace Avalonia.DmaBufInteropTests;

/// <summary>
/// Allocates DMA-BUF buffers with known pixel content for testing.
/// Prefers GBM when available, falls back to udmabuf.
/// </summary>
internal sealed unsafe class DmaBufAllocator : IDisposable
{
    private readonly int _drmFd;
    private readonly IntPtr _gbmDevice;

    public bool IsAvailable => _gbmDevice != IntPtr.Zero;

    public DmaBufAllocator()
    {
        // Try render nodes in order
        foreach (var path in new[] { "/dev/dri/renderD128", "/dev/dri/renderD129" })
        {
            if (!File.Exists(path))
                continue;
            _drmFd = Open(path, O_RDWR);
            if (_drmFd < 0)
                continue;
            _gbmDevice = GbmCreateDevice(_drmFd);
            if (_gbmDevice != IntPtr.Zero)
                return;
            Close(_drmFd);
            _drmFd = -1;
        }
    }

    /// <summary>
    /// Allocates a DMA-BUF with the specified format and fills it with a solid ARGB color.
    /// </summary>
    public DmaBufAllocation? AllocateLinear(uint width, uint height, uint format, uint color)
    {
        if (_gbmDevice == IntPtr.Zero)
            return AllocateViaUdmabuf(width, height, format, color);

        var bo = GbmBoCreate(_gbmDevice, width, height, format,
            GBM_BO_USE_RENDERING | GBM_BO_USE_LINEAR);
        if (bo == IntPtr.Zero)
            return null;

        var fd = GbmBoGetFd(bo);
        var stride = GbmBoGetStride(bo);
        var modifier = GbmBoGetModifier(bo);

        // Map and fill with color
        uint mapStride;
        IntPtr mapData = IntPtr.Zero;
        var mapped = GbmBoMap(bo, 0, 0, width, height, GBM_BO_TRANSFER_WRITE, &mapStride, &mapData);
        if (mapped != IntPtr.Zero)
        {
            var pixels = (uint*)mapped;
            var pixelsPerRow = mapStride / 4;
            for (uint y = 0; y < height; y++)
                for (uint x = 0; x < width; x++)
                    pixels[y * pixelsPerRow + x] = color;
            GbmBoUnmap(bo, mapData);
        }

        return new DmaBufAllocation(fd, stride, modifier, width, height, format, bo);
    }

    /// <summary>
    /// Allocates a DMA-BUF with tiled modifier (if the GPU supports it).
    /// </summary>
    public DmaBufAllocation? AllocateTiled(uint width, uint height, uint format, uint color)
    {
        if (_gbmDevice == IntPtr.Zero)
            return null;

        // Use GBM_BO_USE_RENDERING without LINEAR to let the driver choose tiling
        var bo = GbmBoCreate(_gbmDevice, width, height, format, GBM_BO_USE_RENDERING);
        if (bo == IntPtr.Zero)
            return null;

        var modifier = GbmBoGetModifier(bo);
        // If the driver gave us linear anyway, this isn't a useful tiled test
        if (modifier == 0) // DRM_FORMAT_MOD_LINEAR
        {
            GbmBoDestroy(bo);
            return null;
        }

        var fd = GbmBoGetFd(bo);
        var stride = GbmBoGetStride(bo);

        // Map and fill — for tiled BOs, GBM handles the detiling in map
        uint mapStride;
        IntPtr mapData = IntPtr.Zero;
        var mapped = GbmBoMap(bo, 0, 0, width, height, GBM_BO_TRANSFER_WRITE, &mapStride, &mapData);
        if (mapped != IntPtr.Zero)
        {
            var pixels = (uint*)mapped;
            var pixelsPerRow = mapStride / 4;
            for (uint y = 0; y < height; y++)
                for (uint x = 0; x < width; x++)
                    pixels[y * pixelsPerRow + x] = color;
            GbmBoUnmap(bo, mapData);
        }

        return new DmaBufAllocation(fd, stride, modifier, width, height, format, bo);
    }

    private static DmaBufAllocation? AllocateViaUdmabuf(uint width, uint height, uint format, uint color)
    {
        if (!File.Exists("/dev/udmabuf"))
            return null;

        var stride = width * 4;
        var size = (long)(stride * height);

        var memfd = MemfdCreate("dmabuf-test", MFD_ALLOW_SEALING);
        if (memfd < 0)
            return null;

        if (Ftruncate(memfd, size) != 0)
        {
            Close(memfd);
            return null;
        }

        // Map and fill
        var mapped = Mmap(IntPtr.Zero, (nuint)size, PROT_READ | PROT_WRITE, MAP_SHARED, memfd, 0);
        if (mapped != IntPtr.Zero && mapped != new IntPtr(-1))
        {
            var pixels = (uint*)mapped;
            var count = width * height;
            for (uint i = 0; i < count; i++)
                pixels[i] = color;
            Munmap(mapped, (nuint)size);
        }

        // Create DMA-BUF via udmabuf
        var udmabufFd = Open("/dev/udmabuf", O_RDWR);
        if (udmabufFd < 0)
        {
            Close(memfd);
            return null;
        }

        var createParams = new UdmabufCreate
        {
            Memfd = memfd,
            Flags = 0,
            Offset = 0,
            Size = (ulong)size
        };

        var dmaBufFd = Ioctl(udmabufFd, UDMABUF_CREATE, &createParams);
        Close(udmabufFd);
        Close(memfd);

        if (dmaBufFd < 0)
            return null;

        return new DmaBufAllocation(dmaBufFd, stride, 0 /* LINEAR */, width, height, format, IntPtr.Zero);
    }

    public void Dispose()
    {
        if (_gbmDevice != IntPtr.Zero)
            GbmDeviceDestroy(_gbmDevice);
        if (_drmFd >= 0)
            Close(_drmFd);
    }
}

internal sealed class DmaBufAllocation : IDisposable
{
    public int Fd { get; }
    public uint Stride { get; }
    public ulong Modifier { get; }
    public uint Width { get; }
    public uint Height { get; }
    public uint DrmFourcc { get; }
    private IntPtr _gbmBo;

    public DmaBufAllocation(int fd, uint stride, ulong modifier, uint width, uint height, uint drmFourcc,
        IntPtr gbmBo)
    {
        Fd = fd;
        Stride = stride;
        Modifier = modifier;
        Width = width;
        Height = height;
        DrmFourcc = drmFourcc;
        _gbmBo = gbmBo;
    }

    public void Dispose()
    {
        if (_gbmBo != IntPtr.Zero)
        {
            NativeInterop.GbmBoDestroy(_gbmBo);
            _gbmBo = IntPtr.Zero;
        }
        if (Fd >= 0)
            NativeInterop.Close(Fd);
    }
}
