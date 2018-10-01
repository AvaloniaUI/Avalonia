using System;
using System.ComponentModel;

#if SYSTEM_XAML
using System.Windows.Markup;
#else
using Portable.Xaml.Markup;
#endif

namespace Avalonia.Markup.Xaml
{
    internal static class ServiceProviderExtensions
    {
        public static T GetService<T>(this IServiceProvider s) => (T)s.GetService(typeof(T));

        public static Type ResolveType(this ITypeDescriptorContext ctx, string namespacePrefix, string type)
        {
            var tr = ctx.GetService<IXamlTypeResolver>();

            string name = string.IsNullOrEmpty(namespacePrefix) ? type : $"{namespacePrefix}:{type}";

            return tr?.Resolve(name);
        }
    }
}
