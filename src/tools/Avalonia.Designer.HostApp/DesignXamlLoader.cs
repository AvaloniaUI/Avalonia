using System;
using System.IO;
using System.Reflection;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.XamlIl;

namespace Avalonia.Designer.HostApp
{
    class DesignXamlLoader : AvaloniaXamlLoader.IRuntimeXamlLoader
    {
        public object Load(Stream stream, Assembly localAsm, object o, Uri baseUri, bool designMode)
        {
            return AvaloniaXamlIlRuntimeCompiler.Load(stream, localAsm, o, baseUri, designMode);
        }
    }
}
