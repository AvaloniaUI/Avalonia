using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Styling;
using Xunit;

namespace Avalonia.Themes.UnitTests.Utilities;

public static class ResourceExtensions
{
    public static IEnumerable<ResourceEntry> EnumerateResources(
        this Application application)
    {
        foreach (var r in application.Resources.EnumerateResources(anchor: application))
        {
            yield return r;
        }

        foreach (var style in application.Styles)
        {
            foreach (var r in style.EnumerateResources())
            {
                yield return r;
            }
        }
    }

    public static IEnumerable<ResourceEntry> EnumerateResources(
        this StyledElement styledElement)
    {
        foreach (var r in styledElement.Resources.EnumerateResources(anchor: styledElement))
        {
            yield return r;
        }

        foreach (var style in styledElement.Styles)
        {
            foreach (var r in style.EnumerateResources())
            {
                yield return r;
            }
        }
    }

    public static IEnumerable<ResourceEntry> EnumerateResources(
        this IStyle style)
    {
        if (style is Styles styles)
        {
            foreach (var r in styles.Resources.EnumerateResources(anchor: styles))
            {
                yield return r;
            }
        }
        else if (style is StyleBase styleBase)
        {
            foreach (var r in styleBase.Resources.EnumerateResources(anchor: styleBase))
            {
                yield return r;
            }
        }

        foreach (var childStyle in style.Children)
        {
            foreach (var r in childStyle.EnumerateResources())
            {
                yield return r;
            }
        }
    }

    public static IEnumerable<ResourceEntry> EnumerateResources(
        this IResourceDictionary resourceDictionary)
        => resourceDictionary.EnumerateResources(anchor: resourceDictionary);

    public static IEnumerable<ResourceEntry> EnumerateResources(
        this IResourceProvider resourceProvider, IEnumerable<object> keys)
        => resourceProvider.EnumerateResources(keys, anchor: resourceProvider, themeVariant: null);

    private static IEnumerable<ResourceEntry> EnumerateResources(
        this IResourceDictionary resourceDictionary, object? anchor, ThemeVariant? themeVariant = null)
    {
        foreach (var r in resourceDictionary.EnumerateResources(resourceDictionary.Keys, anchor, themeVariant))
        {
            yield return r;
        }

        foreach (var (variant, variantProvider) in resourceDictionary.ThemeDictionaries)
        {
            foreach (var r in variantProvider.EnumerateResources(anchor, variant))
            {
                yield return r;
            }
        }

        foreach (var mergedProvider in resourceDictionary.MergedDictionaries)
        {
            foreach (var r in mergedProvider.EnumerateResources(anchor, themeVariant))
            {
                yield return r;
            }
        }
    }

    private static IEnumerable<ResourceEntry> EnumerateResources(
        this IResourceProvider resourceProvider, object? anchor, ThemeVariant? themeVariant)
    {
        if (resourceProvider is IResourceDictionary resourceDictionary)
        {
            return resourceDictionary.EnumerateResources(anchor, themeVariant);
        }

        if (KnownResourceProviders.TryGetKnownResourceKeys(resourceProvider) is { } knownKeys)
        {
            return resourceProvider.EnumerateResources(knownKeys, anchor, themeVariant);
        }

        return [];
    }

    private static IEnumerable<ResourceEntry> EnumerateResources(
        this IResourceProvider resourceProvider, IEnumerable<object> keys, object? anchor, ThemeVariant? themeVariant)
    {
        return keys.Select(k =>
        {
            Assert.True(resourceProvider.TryGetResource(k, themeVariant, out var val), $"Key \"{k}\" defined, but value was not found");
  
            return new ResourceEntry(
                Anchor: anchor ?? resourceProvider,
                Provider: resourceProvider,
                Key: k,
                Value: val,
                IsDeferred: (resourceProvider as ResourceDictionary)?.ContainsDeferredKey(k) ?? false,
                ThemeVariant: themeVariant ?? ThemeVariant.Default);
        });
    }

    public record ResourceEntry(
        object Anchor,
        IResourceProvider Provider,
        object Key,
        object? Value,
        bool IsDeferred,
        ThemeVariant ThemeVariant)
    {
        public bool IsReadOnly => Provider is not IResourceDictionary; 
    }
}
