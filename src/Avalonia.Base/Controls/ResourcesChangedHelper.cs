using System;
using Avalonia.LogicalTree;

namespace Avalonia.Controls;

internal static class ResourcesChangedHelper
{
    internal static void NotifyHostedResourcesChanged(this IResourceHost host, ResourcesChangedToken token)
    {
        if (host is IResourceHost2 host2)
            host2.NotifyHostedResourcesChanged(token);
        else
            host.NotifyHostedResourcesChanged(ResourcesChangedEventArgs.Empty);
    }

    internal static void NotifyResourcesChanged(this ILogical logical, ResourcesChangedToken token)
    {
        if (logical is StyledElement styledElement)
            styledElement.NotifyResourcesChanged(token);
        else
            logical.NotifyResourcesChanged(ResourcesChangedEventArgs.Empty);
    }

    internal static void SubscribeToResourcesChanged(
        this IResourceHost host,
        EventHandler<ResourcesChangedEventArgs> handler,
        EventHandler<ResourcesChangedToken> handler2)
    {
        if (host is IResourceHost2 host2)
            host2.ResourcesChanged2 += handler2;
        else
            host.ResourcesChanged += handler;
    }

    internal static void UnsubscribeFromResourcesChanged(
        this IResourceHost host,
        EventHandler<ResourcesChangedEventArgs> handler,
        EventHandler<ResourcesChangedToken> handler2)
    {
        if (host is IResourceHost2 host2)
            host2.ResourcesChanged2 -= handler2;
        else
            host.ResourcesChanged -= handler;
    }
}
