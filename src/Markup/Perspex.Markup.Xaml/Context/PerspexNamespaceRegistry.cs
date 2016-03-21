﻿// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Glass;
using OmniXaml.Builder;
using OmniXaml.Typing;
using Perspex.Controls;
using Perspex.Markup.Xaml.Templates;
using Perspex.Media;
using Perspex.Metadata;
using Perspex.Platform;
using Perspex.Styling;

namespace Perspex.Markup.Xaml.Context
{
    public class PerspexNamespaceRegistry : INamespaceRegistry
    {
        private const string ClrNamespace = "clr-namespace:";
        private const string PerspexNs = "https://github.com/perspex";

        private static readonly IEnumerable<Assembly> ForcedAssemblies = new[]
        {
            typeof(PerspexObject).GetTypeInfo().Assembly,
            typeof(Control).GetTypeInfo().Assembly,
            typeof(Style).GetTypeInfo().Assembly,
            typeof(DataTemplate).GetTypeInfo().Assembly,
            typeof(SolidColorBrush).GetTypeInfo().Assembly,
            typeof(IValueConverter).GetTypeInfo().Assembly,
            typeof(IControl).GetTypeInfo().Assembly
        };

        private List<ClrNamespace> _clrNamespaces = new List<ClrNamespace>();
        private List<XamlNamespace> _namespaces = new List<XamlNamespace>();
        private Dictionary<string, string> _prefixes = new Dictionary<string, string>();
        private List<Assembly> _scanned = new List<Assembly>();

        public PerspexNamespaceRegistry()
        {
            ScanAssemblies(ForcedAssemblies);
            ScanNewAssemblies();
            RegisterPrefix(new PrefixRegistration(string.Empty, PerspexNs));
        }

        public IEnumerable<PrefixRegistration> RegisteredPrefixes => 
            _prefixes.Select(x => new PrefixRegistration(x.Key, x.Value));

        public void AddNamespace(XamlNamespace xamlNamespace)
        {
            _namespaces.Add(xamlNamespace);
        }

        public Namespace GetNamespace(string name)
        {
            Namespace result;

            if (!IsClrNamespace(name))
            {
                ScanNewAssemblies();
                result = _namespaces.FirstOrDefault(x => x.Name == name);

                if (result == null)
                {
                    result = _namespaces.FirstOrDefault(x => x.Name == name);
                }
            }
            else
            {
                result = _clrNamespaces.FirstOrDefault(x => x.Name == name);

                if (result == null)
                {
                    var clr = CreateClrNamespace(name);
                    _clrNamespaces.Add(clr);
                    result = clr;
                }
            }

            return result;
        }

        public Namespace GetNamespaceByPrefix(string prefix)
        {
            string uri;

            if (_prefixes.TryGetValue(prefix, out uri))
            {
                return GetNamespace(uri);
            }

            return null;
        }

        public void RegisterPrefix(PrefixRegistration prefixRegistration)
        {
            _prefixes[prefixRegistration.Prefix] = prefixRegistration.Ns;
        }

        private static bool IsClrNamespace(string ns)
        {
            return ns.StartsWith(ClrNamespace);
        }

        private static ClrNamespace CreateClrNamespace(string formattedClrString)
        {
            var startOfNamespace = formattedClrString.IndexOf(":", StringComparison.Ordinal) + 1;
            var endOfNamespace = formattedClrString.IndexOf(";", startOfNamespace, StringComparison.Ordinal);

            if (endOfNamespace < 0)
            {
                endOfNamespace = formattedClrString.Length - startOfNamespace;
            }

            var ns = formattedClrString.Substring(startOfNamespace, endOfNamespace - startOfNamespace);

            var remainingPartStart = startOfNamespace + ns.Length + 1;
            var remainingPartLenght = formattedClrString.Length - remainingPartStart;
            var assemblyPart = formattedClrString.Substring(remainingPartStart, remainingPartLenght);

            var assembly = GetAssembly(assemblyPart);

            return new ClrNamespace(assembly, ns);
        }

        private static Assembly GetAssembly(string assemblyPart)
        {
            var dicotomize = assemblyPart.Dicotomize('=');
            return Assembly.Load(new AssemblyName(dicotomize.Item2));
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
                    var xamlNamespace = _namespaces.FirstOrDefault(x => x.Name == nsa.Key);

                    if (xamlNamespace == null)
                    {
                        xamlNamespace = new XamlNamespace(nsa.Key);
                        _namespaces.Add(xamlNamespace);
                    }
                        
                    var clrNamespaces = nsa.Select(x => x.ClrNamespace);
                    xamlNamespace.Addresses.Add(new ConfiguredAssemblyWithNamespaces(assembly, clrNamespaces));
                }

                _scanned.Add(assembly);
            }
        }

        private void ScanNewAssemblies()
        {
            IEnumerable<Assembly> assemblies = PerspexLocator.Current
                .GetService<IPclPlatformWrapper>()
                ?.GetLoadedAssemblies();

            if (assemblies != null)
            {
                assemblies = assemblies.Except(_scanned);
                ScanAssemblies(assemblies);
            }
        }
    }
}
