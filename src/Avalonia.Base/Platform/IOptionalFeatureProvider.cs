using System;

namespace Avalonia.Platform;

public interface IOptionalFeatureProvider
{
    /// <summary>
    /// Queries for an optional feature
    /// </summary>
    /// <param name="featureType">Feature type</param>
    public object? TryGetFeature(Type featureType);
}

public static class OptionalFeatureProviderExtensions
{
    public static T? TryGetFeature<T>(this IOptionalFeatureProvider provider) where T : class =>
        (T?)provider.TryGetFeature(typeof(T));
}