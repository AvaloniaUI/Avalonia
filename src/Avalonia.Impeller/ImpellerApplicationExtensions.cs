using Avalonia.Controls;
using Avalonia.Impeller;

// ReSharper disable once CheckNamespace
namespace Avalonia;

/// <summary>
/// Impeller application extensions.
/// </summary>
public static class ImpellerApplicationExtensions
{
    /// <summary>
    /// Enables the Impeller rendering backend. Options can be set with <c>With(new ImpellerOptions())</c>.
    /// </summary>
    /// <param name="builder">The app builder.</param>
    /// <returns>The app builder.</returns>
    public static AppBuilder UseImpeller(this AppBuilder builder)
    {
        return builder.UseRenderingSubsystem(
            () => ImpellerPlatform.Initialize(AvaloniaLocator.Current.GetService<ImpellerOptions>() ??
                                              new ImpellerOptions()),
            "Impeller");
    }
}
