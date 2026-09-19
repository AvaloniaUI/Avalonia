using Avalonia.Platform;

namespace Avalonia.Win32;

/// <summary>
/// An immutable bag with Win32-specific per-frame scene information, published via
/// <see cref="ITopLevelImpl.TopLevelSpecificSceneInfo"/> and consumed by render targets
/// on the render thread.
/// </summary>
internal sealed record Win32TopLevelSceneInfo(PlatformThemeVariant ThemeVariant);
