// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Styling;

namespace Avalonia.Markup.Xaml.Context
{
    using ClrNamespaceInfo = Tuple<string, Assembly>;

    public interface IRuntimeTypeProvider
    {
        Type FindType(string xamlNamespace, string name, Type[] genArgs);

        IEnumerable<Assembly> ReferencedAssemblies { get; }
    }

    public class AvaloniaRuntimeTypeProvider : IRuntimeTypeProvider
    {
        private const string ClrNamespace = "clr-namespace:";
        // private const string AvaloniaNs = "https://github.com/avaloniaui";

        private static readonly IEnumerable<Assembly> ForcedAssemblies = new[]
        {
            typeof(AvaloniaObject).GetTypeInfo().Assembly,
            typeof(Control).GetTypeInfo().Assembly,
            typeof(Style).GetTypeInfo().Assembly,
            typeof(DataTemplate).GetTypeInfo().Assembly,
            typeof(SolidColorBrush).GetTypeInfo().Assembly,
            typeof(IValueConverter).GetTypeInfo().Assembly,
        };

        private Dictionary<string, HashSet<ClrNamespaceInfo>> _namespaces = new Dictionary<string, HashSet<ClrNamespaceInfo>>();

        private List<Assembly> _scanned = new List<Assembly>();

        public IEnumerable<Assembly> ReferencedAssemblies => _scanned;

        public AvaloniaRuntimeTypeProvider()
        {
            ScanAssemblies(ForcedAssemblies);
            ScanNewAssemblies();
        }

        private static bool IsClrNamespace(string ns)
        {
            return ns.StartsWith(ClrNamespace);
        }

        private static Assembly GetAssembly(string assemblyName)
        {
            return Assembly.Load(new AssemblyName(assemblyName));
        }

        private void ScanAssemblies(IEnumerable<Assembly> assemblies)
        {
            foreach (var assembly in assemblies)
            {
                var namespaces = assembly.GetCustomAttributes<XmlnsDefinitionAttribute>()
                    .Select(x => new { x.XmlNamespace, x.ClrNamespace })
                    .GroupBy(x => x.XmlNamespace);

                foreach (var nsa in namespaces)
                {
                    HashSet<ClrNamespaceInfo> reg;

                    if (!_namespaces.TryGetValue(nsa.Key, out reg))
                    {
                        _namespaces[nsa.Key] = reg = new HashSet<Tuple<string, Assembly>>();
                    }

                    foreach (var child in nsa)
                    {
                        reg.Add(new ClrNamespaceInfo(child.ClrNamespace, assembly));
                    }
                }

                _scanned.Add(assembly);
            }
        }

        private void ScanNewAssemblies()
        {
            IEnumerable<Assembly> assemblies = AvaloniaLocator.Current
                .GetService<IRuntimePlatform>()
                ?.GetLoadedAssemblies();

            if (assemblies != null)
            {
                assemblies = assemblies.Except(_scanned);
                ScanAssemblies(assemblies);
            }
        }

        private Dictionary<string, Type> _typeCache = new Dictionary<string, Type>();

        public Type FindType(string xamlNamespace, string name, Type[] genArgs)
        {
            if (IsClrNamespace(xamlNamespace))
            {
                //we need to handle only xaml url namespaces for avalonia,
                //the other namespaces are handled well in portable.xaml
                return null;
            }

            string key = $"{xamlNamespace}:{name}";

            Type type;

            if (_typeCache.TryGetValue(key, out type))
            {
                return type;
            }

            HashSet<ClrNamespaceInfo> reg;

            if (!_namespaces.TryGetValue(xamlNamespace, out reg))
            {
                return null;
            }

            if (genArgs != null)
                name += "`" + genArgs.Length;

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
    }
}