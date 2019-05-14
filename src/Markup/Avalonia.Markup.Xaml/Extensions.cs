using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Portable.Xaml;
using Portable.Xaml.Markup;

namespace Avalonia.Markup.Xaml
{
    internal static class Extensions
    {
        public static T GetService<T>(this IServiceProvider sp) => (T)sp?.GetService(typeof(T));
        
        
        public static Uri GetContextBaseUri(this IServiceProvider ctx)
        {
            var properService = ctx.GetService<IUriContext>();
            if (properService != null)
                return properService.BaseUri;
            // Ugly hack with casts
            return Portable.Xaml.ComponentModel.TypeDescriptorExtensions.GetBaseUri((ITypeDescriptorContext)ctx);
        }

        public static T GetFirstParent<T>(this IServiceProvider ctx) where T : class
        {
            var parentStack = ctx.GetService<IAvaloniaXamlIlParentStackProvider>();
            if (parentStack != null)
                return parentStack.Parents.OfType<T>().FirstOrDefault();
            return Portable.Xaml.ComponentModel.TypeDescriptorExtensions.GetFirstAmbientValue<T>((ITypeDescriptorContext)ctx);
        }
        
        public static T GetLastParent<T>(this IServiceProvider ctx) where T : class
        {
            var parentStack = ctx.GetService<IAvaloniaXamlIlParentStackProvider>();
            if (parentStack != null)
                return parentStack.Parents.OfType<T>().LastOrDefault();
            return Portable.Xaml.ComponentModel.TypeDescriptorExtensions.GetLastOrDefaultAmbientValue<T>(
                (ITypeDescriptorContext)ctx);
        }

        public static IEnumerable<T> GetParents<T>(this IServiceProvider sp)
        {
            var stack = sp.GetService<IAvaloniaXamlIlParentStackProvider>();
            if (stack != null)
                return stack.Parents.OfType<T>();
            
            var context = (ITypeDescriptorContext)sp;
            var schemaContext = context.GetService<IXamlSchemaContextProvider>().SchemaContext;
            var ambientProvider = context.GetService<IAmbientProvider>();
            return ambientProvider.GetAllAmbientValues(schemaContext.GetXamlType(typeof(T))).OfType<T>();
        }

        public static Type ResolveType(this IServiceProvider ctx, string namespacePrefix, string type)
        {
            var tr = ctx.GetService<IXamlTypeResolver>();
            string name = string.IsNullOrEmpty(namespacePrefix) ? type : $"{namespacePrefix}:{type}";
            return tr?.Resolve(name);
        }
    }
}
