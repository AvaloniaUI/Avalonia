using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.UnitTests;

namespace Avalonia.Markup.Xaml.UnitTests
{
    public class XamlTestBase : ScopedTestBase
    {
        public XamlTestBase()
        {
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
