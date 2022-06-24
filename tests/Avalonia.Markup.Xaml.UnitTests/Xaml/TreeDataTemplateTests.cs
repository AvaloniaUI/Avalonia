using System.Linq;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class TreeDataTemplateTests : XamlTestBase
    {
        [Fact]
        public void Binding_Should_Be_Assigned_To_ItemsSource_Instead_Of_Bound()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformWrapper))
            {
                var xaml = "<DataTemplates xmlns='https://github.com/avaloniaui'><TreeDataTemplate DataType='Control' ItemsSource='{Binding}'/></DataTemplates>";
                var templates = (DataTemplates)AvaloniaRuntimeXamlLoader.Load(xaml);
                var template = (TreeDataTemplate)(templates.First());

                Assert.IsType<Binding>(template.ItemsSource);
            }                
        }
        
        [Fact]
        public void XDataType_Should_Be_Assigned_To_Clr_Property()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformWrapper))
            {
                var xaml = @"
<DataTemplates xmlns='https://github.com/avaloniaui'
               xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TreeDataTemplate x:DataType='x:String' />
</DataTemplates>";
                var templates = (DataTemplates)AvaloniaRuntimeXamlLoader.Load(xaml);
                var template = (TreeDataTemplate)(templates.First());

                Assert.Equal(typeof(string), template.DataType);
            }                
        }
    }
}
