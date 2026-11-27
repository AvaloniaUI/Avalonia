using System;
using System.Collections.Generic;
using Avalonia.Logging;
using Avalonia.Platform;
using static Avalonia.OpenGL.Egl.EglConsts;
using static Avalonia.OpenGL.GlConsts;

namespace Avalonia.OpenGL.Egl;

internal partial class EglExternalObjectsFeature
{
    private bool _dmaBufSupported;
    private bool _hasModifiers;
    private bool _dmaBufFormatsQueried;
    private IReadOnlyList<PlatformGraphicsDrmFormat>? _dmaBufFormats;

    private bool TryInitializeDrm()
    {
        try
        {
            var egl = _context.Display.EglInterface;
            if (!egl.IsCreateImageKHRAvailable || !egl.IsDestroyImageKHRAvailable)
                return false;

            var eglExtensions = egl.QueryString(_context.Display.Handle, EGL_EXTENSIONS);
            if (eglExtensions == null || !eglExtensions.Contains("EGL_EXT_image_dma_buf_import"))
                return false;

            if (!_context.GlInterface.GetExtensions().Contains("GL_OES_EGL_image"))
                return false;

            _hasModifiers = eglExtensions.Contains("EGL_EXT_image_dma_buf_import_modifiers");
            _dmaBufSupported = true;
            return true;
        }
        catch (Exception e)
        {
            Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log(this,
                "Unable to initialize EGL dma-buf import feature: " + e);
            return false;
        }
    }

    private static IReadOnlyList<PlatformGraphicsExternalImageFormat> GetSupportedDmaBufImageFormats() =>
        new[]
        {
            PlatformGraphicsExternalImageFormat.B8G8R8A8UNorm,
            PlatformGraphicsExternalImageFormat.R8G8B8A8UNorm
        };

    public IReadOnlyList<PlatformGraphicsDrmFormat>? SupportedDmaBufFormats
    {
        get
        {
            if (!_dmaBufFormatsQueried)
            {
                using (_context.Display.Lock())
                {
                    if (!_dmaBufFormatsQueried)
                    {
                        _dmaBufFormats = TryQueryDmaBufFormats();
                        _dmaBufFormatsQueried = true;
                    }
                }
            }
            return _dmaBufFormats;
        }
    }

    private unsafe IReadOnlyList<PlatformGraphicsDrmFormat>? TryQueryDmaBufFormats()
    {
        var egl = _context.Display.EglInterface;
        var display = _context.Display.Handle;
        if (!_dmaBufSupported || !_hasModifiers
            || !egl.IsQueryDmaBufFormatsEXTAvailable || !egl.IsQueryDmaBufModifiersEXTAvailable)
            return null;

        try
        {
            if (!egl.QueryDmaBufFormatsEXT(display, 0, null, out var formatCount) || formatCount <= 0)
                return null;

            var formats = new int[formatCount];
            fixed (int* formatsPtr = formats)
            {
                if (!egl.QueryDmaBufFormatsEXT(display, formatCount, formatsPtr, out formatCount))
                    return null;
            }

            var result = new List<PlatformGraphicsDrmFormat>();
            for (var f = 0; f < formatCount; f++)
            {
                var format = formats[f];
                if (!egl.QueryDmaBufModifiersEXT(display, format, 0, null, null, out var modifierCount))
                    continue;
                if (modifierCount <= 0)
                {
                    // No explicit modifiers reported: the format imports with the implicit layout.
                    result.Add(new PlatformGraphicsDrmFormat((uint)format,
                        PlatformGraphicsExternalImageDmaBufProperties.DrmModifierInvalid));
                    continue;
                }

                var modifiers = new ulong[modifierCount];
                // EGLBoolean is 4 bytes; back the external-only buffer with ints.
                var externalOnly = new int[modifierCount];
                fixed (ulong* modifiersPtr = modifiers)
                fixed (int* externalOnlyPtr = externalOnly)
                {
                    if (!egl.QueryDmaBufModifiersEXT(display, format, modifierCount, modifiersPtr,
                            (bool*)externalOnlyPtr, out modifierCount))
                        continue;
                }

                for (var m = 0; m < modifierCount; m++)
                {
                    // External-only modifiers can only be sampled via GL_TEXTURE_EXTERNAL_OES;
                    // the import path binds GL_TEXTURE_2D, so they are not usable here.
                    if (externalOnly[m] == 0)
                        result.Add(new PlatformGraphicsDrmFormat((uint)format, modifiers[m]));
                }
            }

            return result;
        }
        catch (Exception e)
        {
            Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log(this,
                "Unable to enumerate EGL dma-buf import formats: " + e);
            return null;
        }
    }

    private IGlExternalImageTexture ImportDmaBufImage(IPlatformHandle handle,
        PlatformGraphicsExternalImageProperties properties)
    {
        if (!_dmaBufSupported)
            throw new ArgumentException(handle.HandleDescriptor + " is not supported", nameof(handle));

        if (properties.DmaBufProperties is not { } dmaBuf)
            throw new ArgumentException(
                $"{nameof(PlatformGraphicsExternalImageProperties.DmaBufProperties)} must be set for dma-buf imports",
                nameof(properties));

        var planeCount = dmaBuf.PlaneCount > 0 ? dmaBuf.PlaneCount : 1;

        var attribs = new List<int>
        {
            EGL_WIDTH, properties.Width,
            EGL_HEIGHT, properties.Height,
            EGL_LINUX_DRM_FOURCC_EXT, (int)dmaBuf.DrmFormat
        };

        for (var p = 0; p < planeCount; p++)
        {
            var fd = dmaBuf.PlaneFds is { } fds ? fds[p] : handle.Handle.ToInt32();
            var offset = dmaBuf.PlaneOffsets is { } offsets ? (int)offsets[p] : (int)properties.MemoryOffset;
            var pitch = dmaBuf.PlaneStrides is { } strides ? (int)strides[p] : 0;

            attribs.Add(PlaneFdAttrib(p));
            attribs.Add(fd);
            attribs.Add(PlaneOffsetAttrib(p));
            attribs.Add(offset);
            attribs.Add(PlanePitchAttrib(p));
            attribs.Add(pitch);

            if (_hasModifiers && dmaBuf.DrmModifier != PlatformGraphicsExternalImageDmaBufProperties.DrmModifierInvalid)
            {
                attribs.Add(PlaneModifierLoAttrib(p));
                attribs.Add((int)(dmaBuf.DrmModifier & 0xFFFFFFFF));
                attribs.Add(PlaneModifierHiAttrib(p));
                attribs.Add((int)(dmaBuf.DrmModifier >> 32));
            }
        }

        attribs.Add(EGL_NONE);

        IntPtr imageHandle;
        using (_context.Display.Lock())
            imageHandle = _context.Display.EglInterface.CreateImageKHR(_context.Display.Handle, IntPtr.Zero,
                EGL_LINUX_DMA_BUF_EXT, IntPtr.Zero, attribs.ToArray());

        if (imageHandle == IntPtr.Zero)
            throw new OpenGlException("eglCreateImageKHR failed to import the dma-buf");

        var eglImage = new EglImage(_context.Display, imageHandle);

        var gl = _context.GlInterface;
        gl.GetIntegerv(GL_TEXTURE_BINDING_2D, out var oldTexture);
        var texture = gl.GenTexture();
        try
        {
            gl.BindTexture(GL_TEXTURE_2D, texture);
            gl.EGLImageTargetTexture2DOES(GL_TEXTURE_2D, eglImage.Handle);
            var err = gl.GetError();
            if (err != 0)
                throw OpenGlException.GetFormattedException("glEGLImageTargetTexture2DOES", err);

            // The imported texture has no mip levels; ensure it is sampling-complete.
            gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
            gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
        }
        catch
        {
            gl.BindTexture(GL_TEXTURE_2D, oldTexture);
            gl.DeleteTexture(texture);
            eglImage.Dispose();
            throw;
        }

        gl.BindTexture(GL_TEXTURE_2D, oldTexture);
        return new DmaBufImageTexture(_context, eglImage, texture, properties);
    }

    private static int PlaneFdAttrib(int plane) => plane switch
    {
        0 => EGL_DMA_BUF_PLANE0_FD_EXT,
        1 => EGL_DMA_BUF_PLANE1_FD_EXT,
        2 => EGL_DMA_BUF_PLANE2_FD_EXT,
        3 => EGL_DMA_BUF_PLANE3_FD_EXT,
        _ => throw new ArgumentOutOfRangeException(nameof(plane))
    };

    private static int PlaneOffsetAttrib(int plane) => plane switch
    {
        0 => EGL_DMA_BUF_PLANE0_OFFSET_EXT,
        1 => EGL_DMA_BUF_PLANE1_OFFSET_EXT,
        2 => EGL_DMA_BUF_PLANE2_OFFSET_EXT,
        3 => EGL_DMA_BUF_PLANE3_OFFSET_EXT,
        _ => throw new ArgumentOutOfRangeException(nameof(plane))
    };

    private static int PlanePitchAttrib(int plane) => plane switch
    {
        0 => EGL_DMA_BUF_PLANE0_PITCH_EXT,
        1 => EGL_DMA_BUF_PLANE1_PITCH_EXT,
        2 => EGL_DMA_BUF_PLANE2_PITCH_EXT,
        3 => EGL_DMA_BUF_PLANE3_PITCH_EXT,
        _ => throw new ArgumentOutOfRangeException(nameof(plane))
    };

    private static int PlaneModifierLoAttrib(int plane) => plane switch
    {
        0 => EGL_DMA_BUF_PLANE0_MODIFIER_LO_EXT,
        1 => EGL_DMA_BUF_PLANE1_MODIFIER_LO_EXT,
        2 => EGL_DMA_BUF_PLANE2_MODIFIER_LO_EXT,
        3 => EGL_DMA_BUF_PLANE3_MODIFIER_LO_EXT,
        _ => throw new ArgumentOutOfRangeException(nameof(plane))
    };

    private static int PlaneModifierHiAttrib(int plane) => plane switch
    {
        0 => EGL_DMA_BUF_PLANE0_MODIFIER_HI_EXT,
        1 => EGL_DMA_BUF_PLANE1_MODIFIER_HI_EXT,
        2 => EGL_DMA_BUF_PLANE2_MODIFIER_HI_EXT,
        3 => EGL_DMA_BUF_PLANE3_MODIFIER_HI_EXT,
        _ => throw new ArgumentOutOfRangeException(nameof(plane))
    };

    private sealed class DmaBufImageTexture : IGlExternalImageTexture
    {
        private readonly EglContext _context;
        private EglImage? _image;
        private int _texture;

        public DmaBufImageTexture(EglContext context, EglImage image, int texture,
            PlatformGraphicsExternalImageProperties properties)
        {
            _context = context;
            _image = image;
            _texture = texture;
            Properties = properties;
        }

        public void Dispose()
        {
            if (_context.IsLost)
                return;
            using (_context.EnsureCurrent())
            {
                if (_texture != 0)
                {
                    _context.GlInterface.DeleteTexture(_texture);
                    _texture = 0;
                }
                _image?.Dispose();
                _image = null;
            }
        }

        public void AcquireKeyedMutex(uint key) => throw new NotSupportedException();
        public void ReleaseKeyedMutex(uint key) => throw new NotSupportedException();

        public int TextureId => _texture;
        public int InternalFormat => GL_RGBA8;
        public int TextureType => GL_TEXTURE_2D;
        public PlatformGraphicsExternalImageProperties Properties { get; }
    }
}
