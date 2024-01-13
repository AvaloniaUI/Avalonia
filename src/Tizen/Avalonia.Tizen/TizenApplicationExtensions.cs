using System.Reflection;
using Avalonia.Platform;

namespace Avalonia.Tizen;

/// <summary>
/// Extension to setup app builder with tizen backend 
/// </summary>
public static class TizenApplicationExtensions
{
    /// <summary>
    /// Use tizen builder to setup tizen sub system
    /// </summary>
    /// <param name="builder">Avalonia App Builder</param>
    /// <returns>Return same builder</returns>
    public static AppBuilder UseTizen(this AppBuilder builder)
    {
        return builder
            .UseTizenRuntimePlatformSubsystem()
            .UseWindowingSubsystem(TizenPlatform.Initialize, "Tizen")
            .UseSkia();
    }
}
