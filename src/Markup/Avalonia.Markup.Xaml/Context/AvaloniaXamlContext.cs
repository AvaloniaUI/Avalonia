using System;
using System.Reflection;
using System.Windows.Markup;
using System.Xaml;

namespace Avalonia.Markup.Xaml.Context
{
    internal class AvaloniaXamlContext : IUriContext
    {
        public AvaloniaXamlContext(
            XamlXmlReaderSettings settings,
            object rootInstance)
        {
            BaseUri = settings.BaseUri;
            LocalAssembly = settings.LocalAssembly;
            RootInstance = rootInstance;
        }

        public Assembly LocalAssembly { get; private set; }
        public Uri BaseUri { get; set; }
        public object RootInstance { get; private set; }
    }
}
