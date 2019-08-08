using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Data;

namespace Avalonia.Markup.Xaml.UnitTests
{
    public class XamlTestBase
    {
        public XamlTestBase()
        {
            // Ensure necessary assemblies are loaded.
            var _ = typeof(TemplateBinding);
        }
    }
}
