using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Markup.Xaml.XamlIl.Runtime;

namespace Avalonia.Markup.Xaml
{
    internal static class Extensions
    {
        public static T GetService<T>(this IServiceProvider sp) => (T)sp?.GetService(typeof(T));
        
        
        public static Uri GetContextBaseUri(this IServiceProvider ctx) => ctx.GetService<IUriContext>().BaseUri;

        public static T GetFirstParent<T>(this IServiceProvider ctx) where T : class 
            => ctx.GetService<IAvaloniaXamlIlParentStackProvider>().Parents.OfType<T>().FirstOrDefault();

        public static T GetLastParent<T>(this IServiceProvider ctx) where T : class 
            => ctx.GetService<IAvaloniaXamlIlParentStackProvider>().Parents.OfType<T>().LastOrDefault();

        public static IEnumerable<T> GetParents<T>(this IServiceProvider sp)
        {
            return sp.GetService<IAvaloniaXamlIlParentStackProvider>().Parents.OfType<T>();
            
            
        }

        public static Type ResolveType(this IServiceProvider ctx, string namespacePrefix, string type)
        {
            var tr = ctx.GetService<IXamlTypeResolver>();
            string name = string.IsNullOrEmpty(namespacePrefix) ? type : $"{namespacePrefix}:{type}";
            return tr?.Resolve(name);
        }
    }
}
