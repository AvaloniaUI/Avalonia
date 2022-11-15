using System;
using System.IO;
using System.Reflection;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.XamlIl;

namespace Avalonia.Designer.HostApp
{
    class DesignXamlLoader : AvaloniaXamlLoader.IRuntimeXamlLoader
    {
        public object Load(Stream stream, RuntimeXamlLoaderConfiguration configuration)
        {
            return AvaloniaXamlIlRuntimeCompiler.Load(stream,
                configuration.LocalAssembly, configuration.RootInstance, configuration.BaseUri,
                configuration.DesignMode, configuration.UseCompiledBindingsByDefault);
        }
    }
}
