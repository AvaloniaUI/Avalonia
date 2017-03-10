using Portable.Xaml;
using System;
using System.Reflection;

namespace Avalonia.Markup.Xaml.PortableXaml
{
    public class AvaloniaXamlContext
    {
        internal AvaloniaXamlContext(Uri baseUri, Assembly localAssembly)
        {
            LocalAssembly = localAssembly;
            BaseUri = baseUri;
        }

        public Assembly LocalAssembly { get; }

        public Uri BaseUri { get; }

        public static implicit operator AvaloniaXamlContext(XamlXmlReaderSettings sett)
        {
            return new AvaloniaXamlContext(sett.BaseUri, sett.LocalAssembly);
        }
    }
}