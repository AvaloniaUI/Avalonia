using Avalonia.Platform;
using Avalonia.Skia;

namespace Avalonia.Impeller;

/// <summary>
/// Impeller platform initializer.
/// </summary>
public static class ImpellerPlatform
{
    /// <summary>
    /// Default DPI.
    /// </summary>
    public static Vector DefaultDpi => new(96.0, 96.0);

    /// <summary>
    /// Initializes the Impeller rendering backend.
    /// </summary>
    public static void Initialize() => Initialize(new ImpellerOptions());

    /// <summary>
    /// Initializes the Impeller rendering backend.
    /// </summary>
    /// <param name="options">The backend options.</param>
    public static void Initialize(ImpellerOptions options)
    {
        AvaloniaLocator.CurrentMutable
            .Bind<IPlatformRenderInterface>().ToConstant(new PlatformRenderInterface(options))
            .Bind<IFontManagerImpl>().ToConstant(new FontManagerImpl());
    }
}
