using System;
using System.Collections.Generic;

namespace Avalonia.OpenGL.Egl;

/// <summary>
/// Given every EGL config matched by eglChooseConfig, returns the one that should be used, or null if none
/// are usable. This lets platforms filter out broken configs that can't be distinguished by their attributes
/// (e.g. nvidia exposes duplicate, partially broken configs) and impose their own preference order between
/// usable ones (e.g. preferring a transparent/32-bit X11 visual, which mesa lists after the opaque ones).
/// </summary>
public delegate IntPtr? EglConfigProbeCallback(EglInterface egl, IntPtr display, IntPtr[] configs);

public class EglDisplayOptions
{
    public EglInterface? Egl { get; set; }
    public bool SupportsContextSharing { get; set; }
    public bool SupportsMultipleContexts { get; set; }

    /// <summary>
    /// Also considers configs that can only be used with PBuffer surfaces. Required for EGL platforms
    /// that have no window surface support at all, e.g. EGL_MESA_platform_surfaceless.
    /// </summary>
    public bool AllowPbufferOnlyConfigs { get; set; }
    public bool ContextLossIsDisplayLoss { get; set; }
    public Func<bool>? DeviceLostCheckCallback { get; set; }
    public Action? DisposeCallback { get; set; }
    public IEnumerable<GlVersion>? GlVersions { get; set; }
    public EglConfigProbeCallback? ProbeConfig { get; set; }

    /// <summary>
    /// Prefers a half float config (EGL_EXT_pixel_format_float) over the ordinary 8 bit one, for
    /// presenting in a colour space such as scRGB that keeps values outside 0..1. Falls back to
    /// the 8 bit config when the driver offers no float one, so this is a preference rather than
    /// a requirement. The context and its surfaces must agree on this: eglMakeCurrent fails with
    /// EGL_BAD_MATCH for a float surface on a fixed point context unless the driver supports
    /// EGL_ANGLE_flexible_surface_compatibility, which most do not.
    /// </summary>
    public bool PreferFloat16Config { get; set; }
}

public class EglContextOptions
{
    public EglContext? ShareWith { get; set; }
    public EglSurface? OffscreenSurface { get; set; }
    public Action? DisposeCallback { get; set; }
    public Dictionary<Type, Func<EglContext, object>>? ExtraFeatures { get; set; }
}

public class EglDisplayCreationOptions : EglDisplayOptions
{
    public int? PlatformType { get; set; }
    public IntPtr PlatformDisplay { get; set; }
    public int[]? PlatformDisplayAttrs { get; set; }
}
