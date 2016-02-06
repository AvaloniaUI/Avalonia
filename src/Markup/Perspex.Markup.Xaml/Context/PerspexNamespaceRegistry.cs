// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        private const string PerspexNs = "https://github.com/perspex";

        private static readonly IEnumerable<Assembly> ForcedAssemblies = new[]
        {
            typeof(PerspexObject).GetTypeInfo().Assembly,
            typeof(Control).GetTypeInfo().Assembly,
            typeof(Style).GetTypeInfo().Assembly,
            typeof(DataTemplate).GetTypeInfo().Assembly,
            typeof(SolidColorBrush).GetTypeInfo().Assembly,
            typeof(IValueConverter).GetTypeInfo().Assembly,
        };

        private List<XamlNamespace> _namespaces = new List<XamlNamespace>();
        private List<PrefixRegistration> _prefixes = new List<PrefixRegistration>();
        private List<Assembly> _scanned = new List<Assembly>();

        public PerspexNamespaceRegistry()
        {
            ScanAssemblies(ForcedAssemblies);
            ScanNewAssemblies();
            RegisterPrefix(new PrefixRegistration(string.Empty, PerspexNs));
        }

        public IEnumerable<PrefixRegistration> RegisteredPrefixes => _prefixes;

        public void AddNamespace(XamlNamespace xamlNamespace)
        {
            _namespaces.Add(xamlNamespace);
        }

        public Namespace GetNamespace(string name)
        {
            var result = _namespaces.FirstOrDefault(x => x.Name == name);

            if (result == null)
            {
                ScanNewAssemblies();
                result = _namespaces.FirstOrDefault(x => x.Name == name);
            }

            return result;
        }

        public Namespace GetNamespaceByPrefix(string prefix)
        {
            var ns = _prefixes.FirstOrDefault(x => x.Prefix == prefix)?.Ns;
            return (ns != null) ? GetNamespace(ns) : null;
        }

        public void RegisterPrefix(PrefixRegistration prefixRegistration)
        {
            _prefixes.Add(prefixRegistration);
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
                        
                    var clrNamespaces = namespaces.SelectMany(x => x).Select(x => x.ClrNamespace);
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
