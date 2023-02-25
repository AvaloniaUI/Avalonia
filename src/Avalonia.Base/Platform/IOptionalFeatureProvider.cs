using System;
using System.Diagnostics.CodeAnalysis;

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

    public static bool TryGetFeature<T>(this IOptionalFeatureProvider provider, [MaybeNullWhen(false)] out T rv)
        where T : class
    {
        rv = provider.TryGetFeature<T>();
        return rv != null;
    }
}
