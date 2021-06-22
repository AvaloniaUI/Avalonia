using System;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class ResourceDictionaryTests : XamlTestBase
    {
        [Fact]
        public void StaticResource_Works_In_ResourceDictionary()
        {
            using (StyledWindow())
            {
                var xaml = @"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <Color x:Key='Red'>Red</Color>
  <SolidColorBrush x:Key='RedBrush' Color='{StaticResource Red}'/>
</ResourceDictionary>";
                var resources = (ResourceDictionary)AvaloniaRuntimeXamlLoader.Load(xaml);
                var brush = (SolidColorBrush)resources["RedBrush"];

                Assert.Equal(Colors.Red, brush.Color);
            }
        }

        [Fact]
        public void DynamicResource_Finds_Resource_In_Parent_Dictionary()
        {
            var dictionaryXaml = @"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <SolidColorBrush x:Key='RedBrush' Color='{DynamicResource Red}'/>
</ResourceDictionary>";

            using (StyledWindow(assets: ("test:dict.xaml", dictionaryXaml)))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceInclude Source='test:dict.xaml'/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
        <Color x:Key='Red'>Red</Color>
    </Window.Resources>
    <Button Name='button' Background='{DynamicResource RedBrush}'/>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var button = window.FindControl<Button>("button");

                var brush = Assert.IsType<SolidColorBrush>(button.Background);
                Assert.Equal(Colors.Red, brush.Color);

                window.Resources["Red"] = Colors.Green;

                Assert.Equal(Colors.Green, brush.Color);
            }
        }

        private IDisposable StyledWindow(params (string, string)[] assets)
        {
            var services = TestServices.StyledWindow.With(
                assetLoader: new MockAssetLoader(assets),
                theme: () => new Styles
                {
                    WindowStyle(),
                });

            return UnitTestApplication.Start(services);
        }

        private Style WindowStyle()
        {
            return new Style(x => x.OfType<Window>())
            {
                Setters =
                {
                    new Setter(
                        Window.TemplateProperty,
                        new FuncControlTemplate<Window>((x, scope) =>
                            new ContentPresenter
                            {
                                Name = "PART_ContentPresenter",
                                [!ContentPresenter.ContentProperty] = x[!Window.ContentProperty],
                            }.RegisterInNameScope(scope)))
                }
            };
        }
    }
}
