using System;
using System.IO;
using System.Reflection;
using Avalonia.DesignerSupport;
using Avalonia.Markup.Xaml.XamlIl;

namespace Avalonia.Designer.HostApp
{
    class DesignXamlLoader : DesignWindowLoader.IDesignXamlLoader
    {
        public object Load(MemoryStream stream, Assembly localAsm, object o, Uri baseUri)
        {
            return AvaloniaXamlIlRuntimeCompiler.Load(stream, localAsm, o, baseUri, true);
        }
    }
}
