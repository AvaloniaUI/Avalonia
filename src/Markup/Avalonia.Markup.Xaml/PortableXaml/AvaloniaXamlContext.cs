using Portable.Xaml;
using Portable.Xaml.Markup;
using System;
using System.Reflection;

namespace Avalonia.Markup.Xaml.PortableXaml
{
    public class AvaloniaXamlContext : IUriContext
    {
        private AvaloniaXamlContext()
        {
        }

        public Assembly LocalAssembly { get; private set; }

        public Uri BaseUri { get; set; }

        public object RootInstance { get; private set; }

        internal static AvaloniaXamlContext For(XamlXmlReaderSettings sett,
                                                object rootInstance)
        {
            return new AvaloniaXamlContext()
            {
                BaseUri = sett.BaseUri,
                LocalAssembly = sett.LocalAssembly,
                RootInstance = rootInstance
            };
        }
    }
}