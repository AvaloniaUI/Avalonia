using System;
using System.IO;
using System.Reflection;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.XamlIl;

namespace Avalonia.Designer.HostApp
{
    class DesignXamlLoader : AvaloniaXamlLoader.IRuntimeXamlLoader
    {
        public object Load(RuntimeXamlLoaderDocument document, RuntimeXamlLoaderConfiguration configuration)
        {
            return AvaloniaXamlIlRuntimeCompiler.Load(document, configuration);
        }
    }
}
