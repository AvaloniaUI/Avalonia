using Avalonia.Metadata;

namespace Avalonia.Controls.ApplicationLifetimes;

/// <summary>
/// Represents an application lifetime where a platform-specific root view is controlled by the 
/// application. This allows Avalonia to be used alongside other native views created by external 
/// components.
/// </summary>
[NotClientImplementable]
public interface IPlatformSingleViewApplicationLifetime : IApplicationLifetime;

/// <inheritdoc cref="IPlatformSingleViewApplicationLifetime"/>
/// <typeparam name="T">The platform-specific type which is expected as the root view element.</typeparam>
[NotClientImplementable]
public interface IPlatformSingleViewApplicationLifetime<T> : IPlatformSingleViewApplicationLifetime
{
    /// <summary>
    /// Gets or sets the root UI element of the application.
    /// </summary>
    T? PlatformView { get; set; }
}
