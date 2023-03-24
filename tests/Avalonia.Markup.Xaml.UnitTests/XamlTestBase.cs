using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Avalonia.Controls;
using Avalonia.Data;

namespace Avalonia.Markup.Xaml.UnitTests
{
    public class XamlTestBase
    {
        public XamlTestBase()
        {
            // Ensure necessary assemblies are loaded.
            var _ = typeof(TemplateBinding);
            GC.KeepAlive(typeof(ItemsRepeater));
            if (AvaloniaLocator.Current.GetService<AvaloniaXamlLoader.IRuntimeXamlLoader>() == null)
                AvaloniaLocator.CurrentMutable.Bind<AvaloniaXamlLoader.IRuntimeXamlLoader>()
                    .ToConstant(new TestXamlLoaderShim());
        }
        
        class TestXamlLoaderShim : AvaloniaXamlLoader.IRuntimeXamlLoader
        {
            public object Load(RuntimeXamlLoaderDocument document, RuntimeXamlLoaderConfiguration configuration) 
                => AvaloniaRuntimeXamlLoader.Load(document, configuration);
        }
    }
}
