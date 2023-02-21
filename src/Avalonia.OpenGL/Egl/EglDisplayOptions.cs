using System;
using System.Collections.Generic;

namespace Avalonia.OpenGL.Egl;

public class EglDisplayOptions
{
    public EglInterface? Egl { get; set; }
    public bool SupportsContextSharing { get; set; }
    public bool SupportsMultipleContexts { get; set; }
    public bool ContextLossIsDisplayLoss { get; set; }
    public Func<bool>? DeviceLostCheckCallback { get; set; }
    public Action? DisposeCallback { get; set; }
    public IEnumerable<GlVersion>? GlVersions { get; set; }
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
