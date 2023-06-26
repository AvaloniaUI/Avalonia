using System.Collections.Generic;
using Avalonia.OpenGL;
using Avalonia.Platform;

namespace Avalonia;

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
    Wgl
}

public enum Win32CompositionMode
{
    /// <summary>
    /// Render Avalonia to a texture inside the Windows.UI.Composition tree.
    /// </summary>
    /// <remarks>
    /// Supported on Windows 10 build 17134 and above. Ignored on other versions.
    /// This is recommended option, as it allows window acrylic effects and high refresh rate rendering.
    /// </remarks>
    WinUIComposition = 1,

    // /// <summary>
    // /// Render Avalonia to a texture inside the DirectComposition tree.
    // /// </summary>
    // /// <remarks>
    // /// Supported on Windows 8 and above. Ignored on other versions.
    // /// </remarks>
    // DirectComposition = 2,

    /// <summary>
    /// When <see cref="LowLatencyDxgiSwapChain"/> is active, renders Avalonia through a low-latency Dxgi Swapchain.
    /// Requires Feature Level 11_3 to be active, Windows 8.1+ Any Subversion. 
    /// This is only recommended if low input latency is desirable, and there is no need for the transparency
    /// and styling / blurring offered by <see cref="WinUIComposition"/><br/>.
    /// </summary>
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
    /// Avalonia window rendering mode.
    /// On Windows 8 and newer default value is <see cref="Win32RenderingMode.AngleEgl"/>,
    /// <see cref="Win32RenderingMode.Software"/> otherwise.
    /// </summary>
    public Win32RenderingMode? RenderingMode { get; set; }

    /// <summary>
    /// Avalonia window composition mode. 
    /// On Windows 8 and newer default value is <see cref="Win32CompositionMode.WinUIComposition"/>,
    /// <see cref="Win32CompositionMode.RedirectionSurface"/> otherwise.
    /// </summary>
    public Win32CompositionMode? CompositionMode { get; set; } 

    /// <summary>
    /// When <see cref="CompositionMode"/> is set to <see cref="Win32CompositionMode.WinUIComposition"/>, create rounded corner blur brushes
    /// If set to null the brushes will be created using default settings (sharp corners)
    /// This can be useful when you need a rounded-corner blurred Windows 10 app, or borderless Windows 11 app.
    /// </summary>
    public float? WinUICompositionBackdropCornerRadius { get; set; }

    /// <summary>
    /// Render directly on the UI thread instead of using a dedicated render thread.
    /// Only applicable if <see cref="CompositionMode"/> is set to <see cref="Win32CompositionMode.None"/>.
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
    /// When this property set <see cref="RenderingMode"/> and <see cref="CompositionMode"/> are completely ignored.
    /// </summary>
    public IPlatformGraphics? CustomPlatformGraphics { get; set; }
}
