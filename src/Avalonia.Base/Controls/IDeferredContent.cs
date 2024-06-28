using System;

namespace Avalonia.Controls;

/// <summary>
/// Represents a deferred content.
/// </summary>
public interface IDeferredContent
{
    /// <summary>
    /// Builds the deferred content using the specified service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider to use.</param>
    /// <returns>The built content.</returns>
    object? Build(IServiceProvider? serviceProvider);
}
