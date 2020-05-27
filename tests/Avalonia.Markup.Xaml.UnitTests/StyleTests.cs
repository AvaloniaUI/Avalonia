using System;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Data;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests
{
    public class StyleTests : XamlTestBase
    {
        [Fact]
        public void Binding_Should_Be_Assigned_To_Setter_Value_Instead_Of_Bound()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformWrapper))
            {
                var xaml = "<Style Selector='Button' xmlns='https://github.com/avaloniaui'><Setter Property='Content' Value='{Binding}'/></Style>";
                var loader = new AvaloniaXamlLoader();
                var style = (Style)loader.Load(xaml);
                var setter = (Setter)(style.Setters.First());

                Assert.IsType<Binding>(setter.Value);
            }                
        }

        [Fact]
        public void Setter_With_TwoWay_Binding_Should_Update_Source()
        {
            using (UnitTestApplication.Start(TestServices.MockThreadingInterface))
            {
                var data = new Data
                {
                    Foo = "foo",
                };

                var control = new TextBox
                {
                    DataContext = data,
                };

                var setter = new Setter
                {
                    Property = TextBox.TextProperty,
                    Value = new Binding
                    {
                        Path = "Foo",
                        Mode = BindingMode.TwoWay
                    }
                };

                setter.Instance(control).Start(false);
                Assert.Equal("foo", control.Text);

                control.Text = "bar";
                Assert.Equal("bar", data.Foo);
            }            
        }

        [Fact]
        public void ResourceInclude_Loads_Resource_Recursively()
        {
            var includeXaml = @"<Style xmlns='https://github.com/avaloniaui'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
       xmlns:sys='clr-namespace:System;assembly=netstandard'>
  <Style.Resources>
    <StaticResource x:Key='Test' ResourceKey='UndefinedResource' />
  </Style.Resources>
</Style>
";

            using (StartWithResources(("test:include.xaml", includeXaml)))
            {
                var xaml = @"<Application xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='ControlCatalog.App'>
  <Application.Styles>
    <StyleInclude Source='test:include.xaml'/>
  </Application.Styles>
</Application>
";

                var loader = new AvaloniaXamlLoader();
                var userControl = (Application)loader.Load(xaml);
            }
        }

        private IDisposable StartWithResources(params (string, string)[] assets)
        {
            var assetLoader = new MockAssetLoader(assets);
            var services = new TestServices(assetLoader: assetLoader);
            return UnitTestApplication.Start(services);
        }

        private class Data
        {
            public string Foo { get; set; }
        }
    }
}
