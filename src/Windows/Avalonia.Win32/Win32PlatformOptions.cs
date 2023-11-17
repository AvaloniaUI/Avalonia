using System.Collections.Generic;
using Avalonia.OpenGL;
using Avalonia.Platform;

namespace Avalonia;

/// <summary>
/// Represents the rendering mode for platform graphics.
/// </summary>
public enum Win32RenderingMode
{
    /// <summary>
    /// Avalonia is rendered into a framebuffer.
    /// </summary>
    Software = 1,

    /// <summary>
    /// Enables ANGLE EGL for Windows with GPU rendering.
    /// </summary>
    AngleEgl = 2,

    /// <summary>
    /// Avalonia would try to use native Widows OpenGL with GPU rendering.
    /// </summary>
    Wgl = 3
}

/// <summary>
/// Represents the DPI Awareness for the application.
/// </summary>
public enum Win32DpiAwareness
{
    /// <summary>
    /// The application is DPI unaware.
    /// </summary>
    Unaware,

    /// <summary>
    /// The application is system DPI aware. It will query DPI once and will not adjust to new DPI changes
    /// </summary>
    SystemDpiAware,

    /// <summary>
    /// The application is per-monitor DPI aware. It adjust its scale factor whenever DPI changes.
    /// </summary>
    PerMonitorDpiAware
}

/// <summary>
/// Represents the Win32 window composition mode.
/// </summary>
public enum Win32CompositionMode
{
    /// <summary>
    /// Render Avalonia to a texture inside the Windows.UI.Composition tree.
    /// </summary>
    /// <remarks>
    /// Supported on Windows 10 build 17134 and above. Ignored on other versions.
    /// This is recommended option, as it allows window acrylic effects and high refresh rate rendering.<br/>
    /// Can only be applied with <see cref="Win32PlatformOptions.RenderingMode"/>=<see cref="Win32RenderingMode.AngleEgl"/>.
    /// </remarks>
    WinUIComposition = 1,

    /// <summary>
    /// Render Avalonia to a texture inside the DirectComposition tree.
    /// </summary>
    /// <remarks>
    /// Supported on Windows 8 and above. Ignored on other versions.<br/>
    /// Can only be applied with <see cref="Win32PlatformOptions.RenderingMode"/>=<see cref="Win32RenderingMode.AngleEgl"/>.
    /// </remarks>
    DirectComposition = 2,

    /// <summary>
    /// When <see cref="LowLatencyDxgiSwapChain"/> is active, renders Avalonia through a low-latency Dxgi Swapchain.
    /// </summary>
    /// <remarks>
    /// Requires Feature Level 11_3 to be active, Windows 8.1+ Any Subversion. 
    /// This is only recommended if low input latency is desirable, and there is no need for the transparency
    /// and styling / blurring offered by <see cref="WinUIComposition"/>.<br/>
    /// Can only be applied with <see cref="Win32PlatformOptions.RenderingMode"/>=<see cref="Win32RenderingMode.AngleEgl"/>.
    /// </remarks>
    LowLatencyDxgiSwapChain = 3,

    /// <summary>
    /// The window renders to a redirection surface.
    /// </summary>
    /// <remarks>
    /// This option is kept only for compatibility with older systems. Some Avalonia features might not work.
    /// </remarks>
    RedirectionSurface,
}

/// <summary>
/// Platform-specific options which apply to Windows.
/// </summary>
public class Win32PlatformOptions
{
    /// <summary>
    /// Embeds popups to the window when set to true. The default value is false.
    /// </summary>
    public bool OverlayPopups { get; set; }

    /// <summary>
    /// Gets or sets Avalonia rendering modes with fallbacks.
    /// The first element in the array has the highest priority.
    /// The default value is: <see cref="Win32RenderingMode.AngleEgl"/>, <see cref="Win32RenderingMode.Software"/>.
    /// </summary>
    /// <remarks>
    /// If application should work on as wide range of devices as possible, at least add <see cref="Win32RenderingMode.Software"/> as a fallback value.
    /// </remarks>
    /// <exception cref="System.InvalidOperationException">Thrown if no values were matched.</exception>
    public IReadOnlyList<Win32RenderingMode> RenderingMode { get; set; } = new[]
    {
        Win32RenderingMode.AngleEgl, Win32RenderingMode.Software
    };

    /// <summary>
    /// Gets or sets Avalonia composition modes with fallbacks.
    /// The first element in the array has the highest priority.
    /// The default value is: <see cref="Win32CompositionMode.WinUIComposition"/>, <see cref="Win32CompositionMode.DirectComposition"/>, <see cref="Win32CompositionMode.RedirectionSurface"/>.
    /// </summary>
    /// <remarks>
    /// If application should work on as wide range of devices as possible, at least add <see cref="Win32CompositionMode.RedirectionSurface"/> as a fallback value.
    /// </remarks>
    /// <exception cref="System.InvalidOperationException">Thrown if no values were matched.</exception>
    public IReadOnlyList<Win32CompositionMode> CompositionMode { get; set; } = new[]
    {
        Win32CompositionMode.WinUIComposition, Win32CompositionMode.DirectComposition, Win32CompositionMode.RedirectionSurface
    };

    /// <summary>
    /// When <see cref="CompositionMode"/> is set to <see cref="Win32CompositionMode.WinUIComposition"/>, create rounded corner blur brushes
    /// If set to null the brushes will be created using default settings (sharp corners)
    /// This can be useful when you need a rounded-corner blurred Windows 10 app, or borderless Windows 11 app.
    /// </summary>
    public float? WinUICompositionBackdropCornerRadius { get; set; }

    /// <summary>
    /// Render directly on the UI thread instead of using a dedicated render thread.
    /// Only applicable if <see cref="CompositionMode"/> is set to <see cref="Win32CompositionMode.RedirectionSurface"/>.
    /// This setting is only recommended for interop with systems that must render on the UI thread, such as WPF.
    /// This setting is false by default.
    /// </summary>
    public bool ShouldRenderOnUIThread { get; set; }

    /// <summary>
    /// Windows OpenGL profiles used when <see cref="RenderingMode"/> is set to <see cref="Win32RenderingMode.Wgl"/>.
    /// This setting is 4.0 and 3.2 by default.
    /// </summary>
    public IList<GlVersion> WglProfiles { get; set; } = new List<GlVersion>
    {
        new(GlProfileType.OpenGL, 4, 0), new(GlProfileType.OpenGL, 3, 2)
    };

    /// <summary>
    /// Provides a way to use a custom-implemented graphics context such as a custom ISkiaGpu.
    /// When this property set <see cref="RenderingMode"/> is ignored
    /// and <see cref="CompositionMode"/> only accepts null or <see cref="Win32CompositionMode.RedirectionSurface"/>.
    /// </summary>
    public IPlatformGraphics? CustomPlatformGraphics { get; set; }

    /// <summary>
    /// Gets or sets the application's DPI awareness.
    /// </summary>
    public Win32DpiAwareness DpiAwareness { get; set; } = Win32DpiAwareness.PerMonitorDpiAware;
}
