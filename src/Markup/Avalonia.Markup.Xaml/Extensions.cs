using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Styling;

namespace Avalonia.Markup.Xaml
{
    internal static class Extensions
    {
        public static T? GetService<T>(this IServiceProvider sp) => (T?)sp.GetService(typeof(T));

        public static T GetRequiredService<T>(this IServiceProvider sp)
            => sp.GetService<T>() ?? throw new InvalidOperationException($"Service {typeof(T)} hasn't been registered");

        public static Uri? GetContextBaseUri(this IServiceProvider ctx) => ctx.GetService<IUriContext>()?.BaseUri;

        public static T? GetFirstParent<T>(this IServiceProvider ctx) where T : class
            => ctx.GetService<IAvaloniaXamlIlParentStackProvider>()?.Parents.OfType<T>().FirstOrDefault();

        public static T? GetLastParent<T>(this IServiceProvider ctx) where T : class
            => ctx.GetService<IAvaloniaXamlIlParentStackProvider>()?.Parents.OfType<T>().LastOrDefault();

        public static IEnumerable<T> GetParents<T>(this IServiceProvider sp)
            => sp.GetService<IAvaloniaXamlIlParentStackProvider>()?.Parents.OfType<T>() ?? Enumerable.Empty<T>();

        public static bool IsInControlTemplate(this IServiceProvider sp) => sp.GetService<IAvaloniaXamlIlControlTemplateProvider>() != null;

        [RequiresUnreferencedCode(TrimmingMessages.XamlTypeResolvedRequiresUnreferenceCodeMessage)]
        public static Type ResolveType(this IServiceProvider ctx, string? namespacePrefix, string type)
        {
            var tr = ctx.GetRequiredService<IXamlTypeResolver>();
            string name = string.IsNullOrEmpty(namespacePrefix) ? type : $"{namespacePrefix}:{type}";
            return tr.Resolve(name);
        }
        
        public static object? GetDefaultAnchor(this IServiceProvider provider)
        {
            // If the target is not a control, so we need to find an anchor that will let us look
            // up named controls and style resources. First look for the closest Control in
            // the context.
            object? anchor = provider.GetFirstParent<Control>();

            if (anchor is null)
            {
                // Try to find IDataContextProvider, this was added to allow us to find
                // a datacontext for Application class when using NativeMenuItems.
                anchor = provider.GetFirstParent<IDataContextProvider>();
            }

            // If a control was not found, then try to find the highest-level style as the XAML
            // file could be a XAML file containing only styles.
            return anchor ??
                   provider.GetService<IRootObjectProvider>()?.RootObject as IStyle ??
                   provider.GetLastParent<IStyle>();
        }
    }
}
