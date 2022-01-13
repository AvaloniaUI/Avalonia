using System.Collections;
using Avalonia.Controls;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.MarkupExtensions
{
    public class MarkupExtensionTests : XamlTestBase
    {
        [Fact]
        public void Markup_Extension_Can_Accept_Nested_Type()
        {
            using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <ListBox Items='{local:EnumToEnumerable {x:Type local:TestViewModel+NestedEnum}}'/>
</Window>";

                var window = AvaloniaRuntimeXamlLoader.Parse<Window>(xaml);
                var listBox = (ListBox)window.Content;
                var items = (IList)listBox.Items;

                Assert.Equal(2, items.Count);
                Assert.Equal(TestViewModel.NestedEnum.Item1, items[0]);
                Assert.Equal(TestViewModel.NestedEnum.Item2, items[1]);
            }
        }
    }
}
