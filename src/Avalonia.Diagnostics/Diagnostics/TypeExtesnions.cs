using System;
using System.Runtime.CompilerServices;
using System.Linq;

namespace Avalonia.Diagnostics
{
    internal static class TypeExtesnions
    {
        private static readonly ConditionalWeakTable<Type, string> s_getTypeNameCache =
            new ConditionalWeakTable<Type, string>();

        public static string GetTypeName(this Type type)
        {
            if (!s_getTypeNameCache.TryGetValue(type, out var name))
            {
                name = type.Name;
                if (Nullable.GetUnderlyingType(type) is Type nullable)
                {
                    name = nullable.Name + "?";
                }
                else if (type.IsGenericType)
                {
                    var definition = type.GetGenericTypeDefinition();
                    var arguments = type.GetGenericArguments();
                    name = definition.Name.Substring(0, definition.Name.IndexOf('`'));
                    name = $"{name}<{string.Join(",", arguments.Select(GetTypeName))}>";
                }
                s_getTypeNameCache.Add(type, name);
            }
            return name;
        }
    }
}
