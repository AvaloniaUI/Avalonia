using Avalonia.Markup.Xaml.PortableXaml;
using Portable.Xaml.Markup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ComponentModel;

namespace Portable.Xaml.ComponentModel
{
    internal static class TypeDescriptorExtensions
    {
        /// <summary>
        /// Gets the service from ITypeDescriptorContext
        /// usually in TypeConverter in xaml reader context
        /// examples:
        /// context.GetService&lt;IXamlTypeResolver&gt;()
        /// context.GetService&lt;IXamlNamespaceResolver&gt;()
        /// context.GetService&lt;IXamlNameProvider&gt;()
        /// context.GetService&lt;INamespacePrefixLookup&gt;()
        /// context.GetService&lt;IXamlSchemaContextProvider&gt;()
        /// context.GetService&lt;IRootObjectProvider&gt;()
        /// context.GetService&lt;IProvideValueTarget&gt;()
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

            // Because GetFirstAmbientValue uses XamlType.CanAssignTo it returns values that
            // aren't actually of the correct type. Use GetAllAmbientValues instead.
            return amb.GetAllAmbientValues(sc.GetXamlType(typeof(T))).OfType<T>().FirstOrDefault();
        }

        public static T GetLastOrDefaultAmbientValue<T>(this ITypeDescriptorContext ctx) where T : class
        {
            return ctx.GetAllAmbientValues<T>().LastOrDefault() as T;
        }

        public static IEnumerable<T> GetAllAmbientValues<T>(this ITypeDescriptorContext ctx) where T : class
        {
            var amb = ctx.GetService<IAmbientProvider>();
            var sc = ctx.GetService<IXamlSchemaContextProvider>().SchemaContext;

            return amb.GetAllAmbientValues(sc.GetXamlType(typeof(T))).OfType<T>();
        }

        public static Uri GetBaseUri(this ITypeDescriptorContext ctx)
        {
            return ctx.GetWriterSettings()?.Context?.BaseUri;
        }

        public static Assembly GetLocalAssembly(this ITypeDescriptorContext ctx)
        {
            return ctx.GetWriterSettings()?.Context?.LocalAssembly;
        }

        public static AvaloniaXamlContext GetAvaloniaXamlContext(this ITypeDescriptorContext ctx)
        {
            return ctx.GetWriterSettings()?.Context;
        }

        public static XamlObjectWriterSettings WithContext(this XamlObjectWriterSettings settings, AvaloniaXamlContext context)
        {
            return new AvaloniaXamlObjectWriterSettings(settings, context);
        }

        private static AvaloniaXamlObjectWriterSettings GetWriterSettings(this ITypeDescriptorContext ctx)
        {
            return ctx.GetService<IXamlObjectWriterFactory>().GetParentSettings() as AvaloniaXamlObjectWriterSettings;
        }

        private class AvaloniaXamlObjectWriterSettings : XamlObjectWriterSettings
        {
            public AvaloniaXamlObjectWriterSettings(XamlObjectWriterSettings settings, AvaloniaXamlContext context)
                : base(settings)
            {
                Context = context;
            }

            public AvaloniaXamlContext Context { get; }
        }
    }
}