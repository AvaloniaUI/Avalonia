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
