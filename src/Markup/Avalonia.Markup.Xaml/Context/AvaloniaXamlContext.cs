using System;
using System.Reflection;

#if SYSTEM_XAML
using System.Xaml;
using System.Windows.Markup;
#else
using Portable.Xaml;
using Portable.Xaml.Markup;
#endif

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
