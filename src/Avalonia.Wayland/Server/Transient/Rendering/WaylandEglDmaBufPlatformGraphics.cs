using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Logging;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Avalonia.Wayland.Server.Interop;
using Avalonia.Wayland.Server.Persistent;
using static Avalonia.OpenGL.Egl.EglConsts;
using static Avalonia.Wayland.Server.Interop.DrmGbmUnsafeNativeMethods;

namespace Avalonia.Wayland.Server.Transient.Rendering;

internal sealed unsafe class WaylandEglDmaBufPlatformGraphics : WaylandPlatformGraphics.IWaylandGraphics
{
    public WaylandEglDisplay Display { get; }

    public IPlatformGraphicsContext CreateContext() => Display.CreateContext(null);

    public IPlatformRenderSurface CreateRenderSurface(WSurface surface) => new WaylandEglDmaBufSurface(surface);

    private WaylandEglDmaBufPlatformGraphics(WaylandEglDisplay display)
    {
        Display = display;
    }

    internal static WaylandEglDmaBufPlatformGraphics? TryCreate(WaylandConnection connection, WaylandGlobals globals,
        IList<GlVersion> glProfiles)
    {
        try
        {
            return Create(connection, globals, glProfiles);
        }
        catch (Exception e)
        {
            Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log(null,
                "Unable to initialize Wayland EGL rendering: {0}", e);
            return null;
        }
    }

    // libEGL.so.1 only exports EGL 1.5 core entry points. Extension entry
    // points like eglGetPlatformDisplayEXT / eglQueryDmaBufFormatsEXT are
    // NOT exported as symbols and can only be resolved via eglGetProcAddress.
    [DllImport("libEGL.so.1", CharSet = CharSet.Ansi)]
    private static extern IntPtr eglGetProcAddress(string proc);

    private static WaylandEglDmaBufPlatformGraphics? Create(WaylandConnection connection, WaylandGlobals globals,
        IList<GlVersion> glProfiles)
    {
        var dmabuf = globals.LinuxDmabuf;
        if (dmabuf == null)
        {
            Logger.TryGet(LogEventLevel.Warning, "OpenGL")?.Log(null,
                "zwp_linux_dmabuf_v1 not available");
            return null;
        }

        // Request default feedback from the compositor
        var feedback = new WaylandDmabufFeedback();
        dmabuf.GetDefaultFeedback(feedback.Listener);
        connection.Queue.Roundtrip();

        if (!feedback.IsComplete)
        {
            Logger.TryGet(LogEventLevel.Warning, "OpenGL")?.Log(null,
                "Dmabuf feedback not received from compositor");
            return null;
        }

        // Find the DRM render node matching the compositor's main device
        var renderNodePath = FindRenderNode(feedback.MainDevice);
        if (renderNodePath == null)
        {
            Logger.TryGet(LogEventLevel.Warning, "OpenGL")?.Log(null,
                "No DRM render node found for device {0}", feedback.MainDevice);
            return null;
        }

        // Open the DRM render node and create GBM device
        var drmFd = open(renderNodePath, O_RDWR);
        if (drmFd < 0)
            throw new InvalidOperationException($"Failed to open DRM render node: {renderNodePath}");

        var gbmDevice = IntPtr.Zero;
        WaylandEglDisplay? display = null;
        try
        {
            gbmDevice = gbm_create_device(drmFd);
            if (gbmDevice == IntPtr.Zero)
                throw new InvalidOperationException("Failed to create GBM device");

            // Capture for the dispose callback — closures capture variables, not values
            var capturedDrmFd = drmFd;
            var capturedGbm = gbmDevice;

            var options = new EglDisplayCreationOptions
            {
                Egl = new EglInterface(eglGetProcAddress),
                PlatformType = EGL_PLATFORM_GBM_KHR,
                PlatformDisplay = gbmDevice,
                SupportsMultipleContexts = true,
                SupportsContextSharing = true,
                DisposeCallback = () =>
                {
                    gbm_device_destroy(capturedGbm);
                    close(capturedDrmFd);
                },
                GlVersions = glProfiles
            };

            display = new WaylandEglDisplay(options, gbmDevice, drmFd, dmabuf);

            var extensions = display.EglInterface.QueryString(display.Handle, EGL_EXTENSIONS)?.Split(' ') ?? [];
            void CheckExtension(string ext)
            {
                if (!extensions.Contains(ext))
                    throw new OpenGlException($"Required EGL extension {ext} is not supported");
            }
            
            CheckExtension("EGL_KHR_image_base");
            CheckExtension("EGL_EXT_image_dma_buf_import");

            var supportedFormats = NegotiateFormats(display, feedback);
            if (supportedFormats.Count == 0)
                supportedFormats.Add(new DmabufFormatModifierPair(DRM_FORMAT_ARGB8888, DRM_FORMAT_MOD_INVALID));
            display.SupportedFormats.AddRange(supportedFormats);

            return new WaylandEglDmaBufPlatformGraphics(display);
        }
        catch
        {
            if (display != null)
            {
                // EglDisplay.Dispose triggers DisposeCallback which handles GBM + DRM fd
                display.Dispose();
            }
            else
            {
                if (gbmDevice != IntPtr.Zero)
                    gbm_device_destroy(gbmDevice);
                close(drmFd);
            }

            throw;
        }
    }

    private static List<DmabufFormatModifierPair> NegotiateFormats(
        EglDisplay display, WaylandDmabufFeedback feedback)
    {
        var egl = display.EglInterface;
        var negotiated = new List<DmabufFormatModifierPair>();

        // Collect all format/modifier pairs from feedback tranches
        var feedbackFormats = new HashSet<(uint format, ulong modifier)>();
        foreach (var tranche in feedback.Tranches)
            foreach (var fmt in tranche.Formats)
                feedbackFormats.Add((fmt.Format, fmt.Modifier));

        if (feedbackFormats.Count == 0)
            return negotiated;

        var uniqueFormats = new HashSet<uint>();
        foreach (var (format, _) in feedbackFormats)
            uniqueFormats.Add(format);

        foreach (var format in uniqueFormats)
        {
            if (!egl.IsQueryDmaBufModifiersEXTAvailable)
            {
                // Modifier query unavailable — only accept implicit modifier
                foreach (var (fmt, mod) in feedbackFormats)
                    if (fmt == format && mod == DRM_FORMAT_MOD_INVALID)
                        negotiated.Add(new DmabufFormatModifierPair(fmt, mod));
                continue;
            }

            // Query how many modifiers EGL supports for this format
            if (!egl.QueryDmaBufModifiersEXT(display.Handle, (int)format, 0, null, null, out var count)
                || count == 0)
                continue;

            // Retrieve the actual modifier list
            var eglModifiers = new ulong[count];
            fixed (ulong* ptr = eglModifiers)
            {
                if (!egl.QueryDmaBufModifiersEXT(display.Handle, (int)format, count, ptr, null, out _))
                    continue;
            }

            var eglModSet = new HashSet<ulong>(eglModifiers);

            // Keep formats whose modifier is supported by EGL or is the implicit modifier
            foreach (var (fmt, mod) in feedbackFormats)
            {
                if (fmt != format)
                    continue;
                if (eglModSet.Contains(mod) || mod == DRM_FORMAT_MOD_INVALID)
                    negotiated.Add(new DmabufFormatModifierPair(fmt, mod));
            }
        }

        return negotiated;
    }

    private static unsafe string? FindRenderNode(ulong mainDevice)
    {
        if (drmGetDeviceFromDevId(mainDevice, 0, out var device) != 0 || device == null)
            return null;

        try
        {
            if ((device->AvailableNodes & (1 << DRM_NODE_RENDER)) == 0)
                return null;

            return Marshal.PtrToStringAnsi(device->Nodes[DRM_NODE_RENDER]);
        }
        finally
        {
            drmFreeDevice(&device);
        }
    }

    public void Dispose()
    {
        Display.Dispose();
    }
}
