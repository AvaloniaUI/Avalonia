using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Styling;
using ClrNamespaceInfo = System.Tuple<string, System.Reflection.Assembly>;

namespace Avalonia.Markup.Xaml.Context
{
    internal class RuntimeTypeProvider : IRuntimeTypeProvider
    {
        private const string ClrNamespace = "clr-namespace:";
        private static readonly IEnumerable<Assembly> s_implicitAssemblies = new[]
        {
            typeof(AvaloniaObject).GetTypeInfo().Assembly,
            typeof(Animation.Animation).GetTypeInfo().Assembly,
            typeof(Control).GetTypeInfo().Assembly,
            typeof(Style).GetTypeInfo().Assembly,
            typeof(DataTemplate).GetTypeInfo().Assembly,
            typeof(SolidColorBrush).GetTypeInfo().Assembly,
            typeof(Binding).GetTypeInfo().Assembly,
        };

        private List<Assembly> _assemblies = new List<Assembly>();
        private Dictionary<string, HashSet<ClrNamespaceInfo>> _namespaces = new Dictionary<string, HashSet<ClrNamespaceInfo>>();
        private Dictionary<string, Type> _typeCache = new Dictionary<string, Type>();

        public IEnumerable<Assembly> ReferencedAssemblies => _assemblies;

        public RuntimeTypeProvider() => ScanAssemblies();

        public Type FindType(string xamlNamespace, string name, IEnumerable<Type> typeArguments)
        {
            if (IsClrNamespace(xamlNamespace))
            {
                return null;
            }

            string key = $"{xamlNamespace}:{name}";

            if (_typeCache.TryGetValue(key, out var type))
            {
                return type;
            }

            if (!_namespaces.TryGetValue(xamlNamespace, out var reg))
            {
                return null;
            }

            if (typeArguments != null)
            {
                name += "`" + typeArguments.Count();
            }

            foreach (var ns in reg)
            {
                var n = ns.Item1 + "." + name;
                var t = ns.Item2.GetType(n);
                if (t != null)
                {
                    _typeCache[key] = t;
                    return t;
                }
            }

            return null;
        }

        private static bool IsClrNamespace(string ns) => ns.StartsWith(ClrNamespace);

        private void ScanAssemblies()
        {
            var assemblies = s_implicitAssemblies.Union(AppDomain.CurrentDomain.GetAssemblies());

            foreach (var assembly in assemblies)
            {
                var namespaces = assembly.GetCustomAttributes<XmlnsDefinitionAttribute>()
                    .Select(x => new { x.XmlNamespace, x.ClrNamespace })
                    .GroupBy(x => x.XmlNamespace);

                foreach (var nsa in namespaces)
                {
                    if (!_namespaces.TryGetValue(nsa.Key, out var reg))
                    {
                        _namespaces[nsa.Key] = reg = new HashSet<Tuple<string, Assembly>>();
                    }

                    foreach (var child in nsa)
                    {
                        reg.Add(new ClrNamespaceInfo(child.ClrNamespace, assembly));
                    }
                }

                _assemblies.Add(assembly);
            }
        }
    }
}
