using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Avalonia.OpenGL.Egl;
using Avalonia.Utilities;
using static Avalonia.LinuxFramebuffer.Output.LibDrm;

namespace Avalonia.LinuxFramebuffer.Output;

internal sealed class GbmEglSwapchain : IDisposable
{
    private readonly DrmCard _card;
    private readonly GbmBoUserDataDestroyCallbackDelegate _destroyFramebuffer;
    private IntPtr _gbmSurface;

    public EglSurface EglSurface { get; }
    public PixelSize Size { get; }
    public bool HasFreeBuffers => gbm_surface_has_free_buffers(_gbmSurface) != 0;
    public bool IsCorrupted { get; set; }

    public GbmEglSwapchain(DrmCard card, IntPtr device, EglDisplay display, PixelSize size)
    {
        _card = card;
        _destroyFramebuffer = (_, data) => drmModeRmFB(_card.Fd, data.ToInt32());
        Size = size;
        _gbmSurface = gbm_surface_create(device, size.Width, size.Height,
            GbmColorFormats.GBM_FORMAT_XRGB8888, GbmBoFlags.GBM_BO_USE_SCANOUT | GbmBoFlags.GBM_BO_USE_RENDERING);
        if (_gbmSurface == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to create GBM surface");

        try
        {
            EglSurface = display.CreateWindowSurface(_gbmSurface);
        }
        catch
        {
            gbm_surface_destroy(_gbmSurface);
            _gbmSurface = IntPtr.Zero;
            throw;
        }
    }

    public static FrontBuffer LockFrontBuffer(IRef<GbmEglSwapchain> swapchain)
    {
        var owner = swapchain.Clone();
        var bo = IntPtr.Zero;
        try
        {
            bo = gbm_surface_lock_front_buffer(owner.Item._gbmSurface);
            if (bo == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to lock GBM front buffer");
            return new FrontBuffer(owner, bo, owner.Item.GetFramebufferId(bo));
        }
        catch
        {
            if (bo != IntPtr.Zero)
                gbm_surface_release_buffer(owner.Item._gbmSurface, bo);
            owner.Dispose();
            throw;
        }
    }

    private uint GetFramebufferId(IntPtr bo)
    {
        var data = gbm_bo_get_user_data(bo);
        if (data != IntPtr.Zero)
            return unchecked((uint)data.ToInt64());

        var width = gbm_bo_get_width(bo);
        var height = gbm_bo_get_height(bo);
        var stride = gbm_bo_get_stride(bo);
        var handle = gbm_bo_get_handle(bo).u32;
        var handles = new[] { handle, 0u, 0u, 0u };
        var pitches = new[] { stride, 0u, 0u, 0u };
        var offsets = new uint[4];
        var result = drmModeAddFB2(_card.Fd, width, height, gbm_bo_get_format(bo),
            handles, pitches, offsets, out var framebuffer, 0);
        if (result != 0)
        {
            result = drmModeAddFB(_card.Fd, width, height, 24, 32, stride, handle, out framebuffer);
            if (result != 0)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to create DRM framebuffer");
        }

        gbm_bo_set_user_data(bo, new IntPtr(unchecked((int)framebuffer)), _destroyFramebuffer);
        return framebuffer;
    }

    public void Dispose()
    {
        if (_gbmSurface == IntPtr.Zero)
            return;

        // Front-buffer leases must outlive scanout and keep this swapchain referenced until then.
        EglSurface.Dispose();
        gbm_surface_destroy(_gbmSurface);
        _gbmSurface = IntPtr.Zero;
        GC.KeepAlive(_destroyFramebuffer);
    }

    internal sealed class FrontBuffer : IDisposable
    {
        private IRef<GbmEglSwapchain>? _swapchain;
        private readonly IntPtr _bo;

        public uint FramebufferId { get; }

        public FrontBuffer(IRef<GbmEglSwapchain> swapchain, IntPtr bo, uint framebufferId)
        {
            _swapchain = swapchain;
            _bo = bo;
            FramebufferId = framebufferId;
        }

        public void Dispose()
        {
            if (_swapchain is not { } owner)
                return;
            _swapchain = null;
            try
            {
                gbm_surface_release_buffer(owner.Item._gbmSurface, _bo);
            }
            finally
            {
                owner.Dispose();
            }
        }
    }
}
