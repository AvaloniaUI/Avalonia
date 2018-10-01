using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

#if SYSTEM_XAML
using System.Xaml;
#else
using Portable.Xaml;
#endif

namespace Avalonia.Markup.Xaml
{
    internal static class TypeDescriptorContextExtenions
    {
        public static IEnumerable<T> GetAllAmbientValues<T>(this ITypeDescriptorContext ctx) where T : class
        {
            var amb = ctx.GetService<IAmbientProvider>();
            var sc = ctx.GetService<IXamlSchemaContextProvider>().SchemaContext;
            return amb.GetAllAmbientValues(sc.GetXamlType(typeof(T))).Cast<T>();
        }

        public static T GetFirstAmbientValue<T>(this ITypeDescriptorContext ctx) where T : class
        {
            var amb = ctx.GetService<IAmbientProvider>();
            var sc = ctx.GetService<IXamlSchemaContextProvider>().SchemaContext;
            return (T)amb.GetFirstAmbientValue(sc.GetXamlType(typeof(T)));
        }

        public static T GetLastOrDefaultAmbientValue<T>(this ITypeDescriptorContext ctx) where T : class
        {
            return ctx.GetAllAmbientValues<T>().LastOrDefault() as T;
        }
    }
}
