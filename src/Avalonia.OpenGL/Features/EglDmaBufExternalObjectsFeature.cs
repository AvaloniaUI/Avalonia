using System;
using System.Collections.Generic;
using Avalonia.Logging;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.SourceGenerator;
using static Avalonia.OpenGL.Egl.EglConsts;
using static Avalonia.OpenGL.GlConsts;

namespace Avalonia.OpenGL.Features;

/// <summary>
/// GL extension interface for GL_OES_EGL_image (binding EGL images as GL textures).
/// </summary>
unsafe partial class EglImageGlInterface
{
    public EglImageGlInterface(Func<string, IntPtr> getProcAddress)
    {
        Initialize(getProcAddress);
    }

    [GetProcAddress("glEGLImageTargetTexture2DOES")]
    public partial void EGLImageTargetTexture2DOES(int target, IntPtr image);
}

/// <summary>
/// Implements <see cref="IGlContextExternalObjectsFeature"/> for DMA-BUF import via the EGL image path.
/// This uses <c>EGL_EXT_image_dma_buf_import</c> and <c>GL_OES_EGL_image</c> rather than the
/// <c>GL_EXT_memory_object_fd</c> path used by <see cref="ExternalObjectsOpenGlExtensionFeature"/>.
/// </summary>
public class EglDmaBufExternalObjectsFeature : IGlContextExternalObjectsFeature
{
    private readonly EglContext _context;
    private readonly EglInterface _egl;
    private readonly EglImageGlInterface _glExt;
    private readonly IntPtr _eglDisplay;
    private readonly bool _hasModifiers;
    private readonly bool _hasSyncFence;

    public static EglDmaBufExternalObjectsFeature? TryCreate(EglContext context)
    {
        var egl = context.EglInterface;
        var display = context.Display.Handle;
        var extensions = egl.QueryString(display, EGL_EXTENSIONS);
        if (extensions == null)
            return null;

        if (!extensions.Contains("EGL_EXT_image_dma_buf_import"))
            return null;

        if (!extensions.Contains("EGL_KHR_image_base"))
            return null;

        var glExtensions = context.GlInterface.GetExtensions();
        if (!glExtensions.Contains("GL_OES_EGL_image"))
            return null;

        try
        {
            return new EglDmaBufExternalObjectsFeature(context, extensions);
        }
        catch (Exception e)
        {
            Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log(nameof(EglDmaBufExternalObjectsFeature),
                "Unable to initialize EGL DMA-BUF import: " + e);
            return null;
        }
    }

    private EglDmaBufExternalObjectsFeature(EglContext context, string extensions)
    {
        _context = context;
        _egl = context.EglInterface;
        _eglDisplay = context.Display.Handle;
        _glExt = new EglImageGlInterface(context.GlInterface.GetProcAddress);
        _hasModifiers = extensions.Contains("EGL_EXT_image_dma_buf_import_modifiers");
        _hasSyncFence = extensions.Contains("EGL_ANDROID_native_fence_sync");
    }

    public IReadOnlyList<string> SupportedImportableExternalImageTypes { get; } =
        new[] { KnownPlatformGraphicsExternalImageHandleTypes.DmaBufFileDescriptor };

    public IReadOnlyList<string> SupportedExportableExternalImageTypes { get; } = Array.Empty<string>();

    public IReadOnlyList<string> SupportedImportableExternalSemaphoreTypes =>
        _hasSyncFence
            ? new[] { KnownPlatformGraphicsExternalSemaphoreHandleTypes.SyncFileDescriptor }
            : Array.Empty<string>();

    public IReadOnlyList<string> SupportedExportableExternalSemaphoreTypes { get; } = Array.Empty<string>();

    public IReadOnlyList<PlatformGraphicsExternalImageFormat> GetSupportedFormatsForExternalMemoryType(string type)
    {
        return new[]
        {
            PlatformGraphicsExternalImageFormat.R8G8B8A8UNorm,
            PlatformGraphicsExternalImageFormat.B8G8R8A8UNorm
        };
    }

    public IGlExportableExternalImageTexture CreateImage(string type, PixelSize size,
        PlatformGraphicsExternalImageFormat format) =>
        throw new NotSupportedException();

    public IGlExportableExternalImageTexture CreateSemaphore(string type) => throw new NotSupportedException();

    public unsafe IGlExternalImageTexture ImportImage(IPlatformHandle handle,
        PlatformGraphicsExternalImageProperties properties)
    {
        if (handle.HandleDescriptor != KnownPlatformGraphicsExternalImageHandleTypes.DmaBufFileDescriptor)
            throw new ArgumentException($"Handle type {handle.HandleDescriptor} is not supported; expected DMABUF_FD");

        // Build EGL attribute list for DMA-BUF import
        var attribs = new List<int>
        {
            EGL_WIDTH, properties.Width,
            EGL_HEIGHT, properties.Height,
            EGL_LINUX_DRM_FOURCC_EXT, (int)properties.DrmFourcc,
            EGL_DMA_BUF_PLANE0_FD_EXT, handle.Handle.ToInt32(),
            EGL_DMA_BUF_PLANE0_OFFSET_EXT, (int)properties.MemoryOffset,
            EGL_DMA_BUF_PLANE0_PITCH_EXT, (int)properties.RowPitch,
        };

        // Add modifier if available
        if (_hasModifiers && properties.DrmModifier != PlatformGraphicsDrmFormats.DRM_FORMAT_MOD_INVALID)
        {
            attribs.Add(EGL_DMA_BUF_PLANE0_MODIFIER_LO_EXT);
            attribs.Add((int)(properties.DrmModifier & 0xFFFFFFFF));
            attribs.Add(EGL_DMA_BUF_PLANE0_MODIFIER_HI_EXT);
            attribs.Add((int)(properties.DrmModifier >> 32));
        }

        // Add additional planes if present
        if (properties.AdditionalPlanes != null)
        {
            for (int i = 0; i < properties.AdditionalPlanes.Length && i < 3; i++)
            {
                var plane = properties.AdditionalPlanes[i];
                switch (i)
                {
                    case 0:
                        attribs.Add(EGL_DMA_BUF_PLANE1_FD_EXT);
                        attribs.Add(plane.Fd);
                        attribs.Add(EGL_DMA_BUF_PLANE1_OFFSET_EXT);
                        attribs.Add((int)plane.Offset);
                        attribs.Add(EGL_DMA_BUF_PLANE1_PITCH_EXT);
                        attribs.Add((int)plane.Pitch);
                        if (_hasModifiers)
                        {
                            attribs.Add(EGL_DMA_BUF_PLANE1_MODIFIER_LO_EXT);
                            attribs.Add((int)(plane.Modifier & 0xFFFFFFFF));
                            attribs.Add(EGL_DMA_BUF_PLANE1_MODIFIER_HI_EXT);
                            attribs.Add((int)(plane.Modifier >> 32));
                        }
                        break;
                    case 1:
                        attribs.Add(EGL_DMA_BUF_PLANE2_FD_EXT);
                        attribs.Add(plane.Fd);
                        attribs.Add(EGL_DMA_BUF_PLANE2_OFFSET_EXT);
                        attribs.Add((int)plane.Offset);
                        attribs.Add(EGL_DMA_BUF_PLANE2_PITCH_EXT);
                        attribs.Add((int)plane.Pitch);
                        if (_hasModifiers)
                        {
                            attribs.Add(EGL_DMA_BUF_PLANE2_MODIFIER_LO_EXT);
                            attribs.Add((int)(plane.Modifier & 0xFFFFFFFF));
                            attribs.Add(EGL_DMA_BUF_PLANE2_MODIFIER_HI_EXT);
                            attribs.Add((int)(plane.Modifier >> 32));
                        }
                        break;
                    case 2:
                        attribs.Add(EGL_DMA_BUF_PLANE3_FD_EXT);
                        attribs.Add(plane.Fd);
                        attribs.Add(EGL_DMA_BUF_PLANE3_OFFSET_EXT);
                        attribs.Add((int)plane.Offset);
                        attribs.Add(EGL_DMA_BUF_PLANE3_PITCH_EXT);
                        attribs.Add((int)plane.Pitch);
                        if (_hasModifiers)
                        {
                            attribs.Add(EGL_DMA_BUF_PLANE3_MODIFIER_LO_EXT);
                            attribs.Add((int)(plane.Modifier & 0xFFFFFFFF));
                            attribs.Add(EGL_DMA_BUF_PLANE3_MODIFIER_HI_EXT);
                            attribs.Add((int)(plane.Modifier >> 32));
                        }
                        break;
                }
            }
        }

        attribs.Add(EGL_NONE);

        // Create EGL image from DMA-BUF
        var eglImage = _egl.CreateImageKHR(_eglDisplay, IntPtr.Zero, EGL_LINUX_DMA_BUF_EXT,
            IntPtr.Zero, attribs.ToArray());

        if (eglImage == IntPtr.Zero)
        {
            var error = _egl.GetError();
            throw OpenGlException.GetFormattedEglException("eglCreateImageKHR (DMA-BUF)", error);
        }

        // Bind as GL texture
        var gl = _context.GlInterface;
        gl.GetIntegerv(GL_TEXTURE_BINDING_2D, out var oldTexture);

        var texture = gl.GenTexture();
        gl.BindTexture(GL_TEXTURE_2D, texture);
        _glExt.EGLImageTargetTexture2DOES(GL_TEXTURE_2D, eglImage);

        var err = gl.GetError();
        gl.BindTexture(GL_TEXTURE_2D, oldTexture);

        if (err != 0)
        {
            gl.DeleteTexture(texture);
            _egl.DestroyImageKHR(_eglDisplay, eglImage);
            throw OpenGlException.GetFormattedException("glEGLImageTargetTexture2DOES", err);
        }

        return new DmaBufImageTexture(_context, _egl, _eglDisplay, properties, texture, eglImage);
    }

    public IGlExternalSemaphore ImportSemaphore(IPlatformHandle handle)
    {
        if (handle.HandleDescriptor != KnownPlatformGraphicsExternalSemaphoreHandleTypes.SyncFileDescriptor)
            throw new ArgumentException($"Handle type {handle.HandleDescriptor} is not supported");

        if (!_hasSyncFence)
            throw new NotSupportedException("EGL_ANDROID_native_fence_sync is not available");

        return new SyncFenceSemaphore(_context, _egl, _eglDisplay, handle.Handle.ToInt32());
    }

    public CompositionGpuImportedImageSynchronizationCapabilities GetSynchronizationCapabilities(
        string imageHandleType)
    {
        if (imageHandleType == KnownPlatformGraphicsExternalImageHandleTypes.DmaBufFileDescriptor)
        {
            var caps = CompositionGpuImportedImageSynchronizationCapabilities.Automatic;
            if (_hasSyncFence)
                caps |= CompositionGpuImportedImageSynchronizationCapabilities.Semaphores;
            return caps;
        }
        return default;
    }

    public byte[]? DeviceLuid => null;
    public byte[]? DeviceUuid => null;

    private class DmaBufImageTexture : IGlExternalImageTexture
    {
        private readonly IGlContext _context;
        private readonly EglInterface _egl;
        private readonly IntPtr _eglDisplay;
        private IntPtr _eglImage;

        public DmaBufImageTexture(IGlContext context, EglInterface egl, IntPtr eglDisplay,
            PlatformGraphicsExternalImageProperties properties, int textureId, IntPtr eglImage)
        {
            _context = context;
            _egl = egl;
            _eglDisplay = eglDisplay;
            _eglImage = eglImage;
            TextureId = textureId;
            Properties = properties;
        }

        public void Dispose()
        {
            if (_context.IsLost)
                return;
            using (_context.EnsureCurrent())
            {
                _context.GlInterface.DeleteTexture(TextureId);
                if (_eglImage != IntPtr.Zero)
                {
                    _egl.DestroyImageKHR(_eglDisplay, _eglImage);
                    _eglImage = IntPtr.Zero;
                }
            }
        }

        public void AcquireKeyedMutex(uint key) => throw new NotSupportedException();
        public void ReleaseKeyedMutex(uint key) => throw new NotSupportedException();

        public int TextureId { get; }
        public int InternalFormat => GL_RGBA8;
        public int TextureType => GL_TEXTURE_2D;
        public PlatformGraphicsExternalImageProperties Properties { get; }
    }

    /// <summary>
    /// Wraps an EGL sync fence (EGL_ANDROID_native_fence_sync) as an <see cref="IGlExternalSemaphore"/>.
    /// The sync fd is imported on construction and waited on GPU-side when WaitSemaphore is called.
    /// </summary>
    private class SyncFenceSemaphore : IGlExternalSemaphore
    {
        private readonly IGlContext _context;
        private readonly EglInterface _egl;
        private readonly IntPtr _eglDisplay;
        private IntPtr _sync;

        public SyncFenceSemaphore(IGlContext context, EglInterface egl, IntPtr eglDisplay, int fd)
        {
            _context = context;
            _egl = egl;
            _eglDisplay = eglDisplay;

            // Import the sync fd as an EGL native fence sync
            var attribs = new[]
            {
                EGL_SYNC_NATIVE_FENCE_FD_ANDROID, fd,
                EGL_NONE
            };
            _sync = egl.CreateSyncKHR(eglDisplay, EGL_SYNC_NATIVE_FENCE_ANDROID, attribs);
            if (_sync == IntPtr.Zero)
                throw OpenGlException.GetFormattedEglException("eglCreateSyncKHR (SYNC_FD import)", egl.GetError());
        }

        public void Dispose()
        {
            if (_sync != IntPtr.Zero && !_context.IsLost)
            {
                _egl.DestroySyncKHR(_eglDisplay, _sync);
                _sync = IntPtr.Zero;
            }
        }

        public void WaitSemaphore(IGlExternalImageTexture texture)
        {
            if (_sync == IntPtr.Zero)
                return;
            // GPU-side wait, no CPU block
            _egl.WaitSyncKHR(_eglDisplay, _sync, 0);
        }

        public void SignalSemaphore(IGlExternalImageTexture texture)
        {
            // Signal is a no-op for imported sync fences — they are consumed on wait
        }

        public void WaitTimelineSemaphore(IGlExternalImageTexture texture, ulong value) =>
            throw new NotSupportedException("SYNC_FD does not support timeline semaphores");

        public void SignalTimelineSemaphore(IGlExternalImageTexture texture, ulong value) =>
            throw new NotSupportedException("SYNC_FD does not support timeline semaphores");
    }
}
