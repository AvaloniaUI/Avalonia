using System;
using Portable.Xaml.Markup;

namespace Portable.Xaml.ComponentModel
{
    internal static class TypeDescriptorExtensions
    {
        /// <summary>
        /// Gets the service from ITypeDescriptorContext
        /// usually in TypeConverter in xaml reader context
        /// examples:
        /// context.GetService<IXamlTypeResolver>()
        /// context.GetService<IXamlNamespaceResolver>()
        /// context.GetService<IXamlNameProvider>()
        /// context.GetService<INamespacePrefixLookup>()
        /// context.GetService<IXamlSchemaContextProvider>()
        /// context.GetService<IRootObjectProvider>()
        /// context.GetService<IProvideValueTarget>()
        /// </summary>
        /// <typeparam name="T">Service Type</typeparam>
        /// <param name="ctx">The TypeDescriptor context.</param>
        /// <returns></returns>
        public static T GetService<T>(this ITypeDescriptorContext ctx) where T : class
        {
            return ctx.GetService(typeof(T)) as T;
        }

        public static Type ResolveType(this ITypeDescriptorContext ctx, string namespacePrefix, string type)
        {
            var tr = ctx.GetService<IXamlTypeResolver>();

            string name = string.IsNullOrEmpty(namespacePrefix) ? type : $"{namespacePrefix}:{type}";

            return tr?.Resolve(name);
        }
    }
}