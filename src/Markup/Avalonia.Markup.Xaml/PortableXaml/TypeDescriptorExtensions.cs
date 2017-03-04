using System;
using Portable.Xaml.Markup;
using System.Linq;
using System.Collections.Generic;

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

        public static T GetFirstAmbientValue<T>(this ITypeDescriptorContext ctx) where T : class
        {
            var amb = ctx.GetService<IAmbientProvider>();
            var sc = ctx.GetService<IXamlSchemaContextProvider>().SchemaContext;

            return amb.GetFirstAmbientValue(sc.GetXamlType(typeof(T))) as T;
        }

        public static T GetLastOrDefaultAmbientValue<T>(this ITypeDescriptorContext ctx) where T : class
        {
            return ctx.GetAllambientValues<T>().LastOrDefault() as T;
        }

        public static IEnumerable<T> GetAllambientValues<T>(this ITypeDescriptorContext ctx) where T : class
        {
            var amb = ctx.GetService<IAmbientProvider>();
            var sc = ctx.GetService<IXamlSchemaContextProvider>().SchemaContext;

            return amb.GetAllAmbientValues(sc.GetXamlType(typeof(T))).OfType<T>();
        }
    }
}