using System;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Media.Immutable;
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

        [Fact]
        public void Item_Is_Added_To_ResourceDictionary_As_Deferred()
        {
            using (StyledWindow())
            {
                var xaml = @"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <SolidColorBrush x:Key='Red' Color='Red' />
</ResourceDictionary>";
                var resources = (ResourceDictionary)AvaloniaRuntimeXamlLoader.Load(xaml);

                Assert.True(resources.ContainsDeferredKey("Red"));
            }
        }
        
        [Fact]
        public void Item_Added_To_ResourceDictionary_Is_UnDeferred_On_Read()
        {
            using (StyledWindow())
            {
                var xaml = @"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <SolidColorBrush x:Key='Red' Color='Red' />
</ResourceDictionary>";
                var resources = (ResourceDictionary)AvaloniaRuntimeXamlLoader.Load(xaml);

                Assert.True(resources.ContainsDeferredKey("Red"));

                Assert.IsType<SolidColorBrush>(resources["Red"]);
                
                Assert.False(resources.ContainsDeferredKey("Red"));
            }
        }

        [Fact]
        public void Item_Is_Added_To_Window_Resources_As_Deferred()
        {
            using (StyledWindow())
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <SolidColorBrush x:Key='Red' Color='Red' />
    </Window.Resources>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var resources = (ResourceDictionary)window.Resources;

                Assert.True(resources.ContainsDeferredKey("Red"));
            }
        }

        [Fact]
        public void Item_Is_Added_To_Window_MergedDictionaries_As_Deferred()
        {
            using (StyledWindow())
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary>
                    <SolidColorBrush x:Key='Red' Color='Red' />
                </ResourceDictionary>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Window.Resources>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var resources = (ResourceDictionary)window.Resources.MergedDictionaries[0];

                Assert.True(resources.ContainsDeferredKey("Red"));
            }
        }

        [Fact]
        public void Item_Is_Added_To_Style_Resources_As_Deferred()
        {
            using (StyledWindow())
            {
                var xaml = @"
<Style xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Style.Resources>
        <SolidColorBrush x:Key='Red' Color='Red' />
    </Style.Resources>
</Style>";
                var style = (Style)AvaloniaRuntimeXamlLoader.Load(xaml);
                var resources = (ResourceDictionary)style.Resources;

                Assert.True(resources.ContainsDeferredKey("Red"));
            }
        }

        [Fact]
        public void Item_Is_Added_To_Styles_Resources_As_Deferred()
        {
            using (StyledWindow())
            {
                var xaml = @"
<Styles xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Styles.Resources>
        <SolidColorBrush x:Key='Red' Color='Red' />
    </Styles.Resources>
</Styles>";
                var style = (Styles)AvaloniaRuntimeXamlLoader.Load(xaml);
                var resources = (ResourceDictionary)style.Resources;

                Assert.True(resources.ContainsDeferredKey("Red"));
            }
        }

        [Fact]
        public void Item_Can_Be_StaticReferenced_As_Deferred()
        {
            using (StyledWindow())
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <SolidColorBrush x:Key='Red' Color='Red' />
    </Window.Resources>
    <Button>
        <Button.Resources>
            <StaticResource x:Key='Red2' ResourceKey='Red' />
        </Button.Resources>
    </Button>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var windowResources = (ResourceDictionary)window.Resources;
                var buttonResources = (ResourceDictionary)((Button)window.Content!).Resources;
                
                Assert.True(windowResources.ContainsDeferredKey("Red"));
                Assert.True(buttonResources.ContainsDeferredKey("Red2"));
            }
        }
        
        [Fact]
        public void Item_StaticReferenced_Is_UnDeferred_On_Read()
        {
            using (StyledWindow())
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <SolidColorBrush x:Key='Red' Color='Red' />
    </Window.Resources>
    <Button>
        <Button.Resources>
            <StaticResource x:Key='Red2' ResourceKey='Red' />
        </Button.Resources>
    </Button>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var windowResources = (ResourceDictionary)window.Resources;
                var buttonResources = (ResourceDictionary)((Button)window.Content!).Resources;
                
                Assert.IsType<SolidColorBrush>(buttonResources["Red2"]);
                
                Assert.False(windowResources.ContainsDeferredKey("Red"));
                Assert.False(buttonResources.ContainsDeferredKey("Red2"));
            }
        }
        
        [Fact]
        public void Value_Type_With_Parse_Converter_Should_Not_Be_Deferred()
        {
            using (StyledWindow())
            {
                var xaml = @"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Color x:Key='Red'>Red</Color>
</ResourceDictionary>";
                var resources = (ResourceDictionary)AvaloniaRuntimeXamlLoader.Load(xaml);

                Assert.False(resources.ContainsDeferredKey("Red"));
                Assert.IsType<Color>(resources["Red"]);
            }
        }
        
        [Fact]
        public void Value_Type_With_Ctor_Converter_Should_Not_Be_Deferred()
        {
            using (StyledWindow())
            {
                var xaml = @"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Thickness x:Key='Margin'>1 1 1 1</Thickness>
</ResourceDictionary>";
                var resources = (ResourceDictionary)AvaloniaRuntimeXamlLoader.Load(xaml);

                Assert.False(resources.ContainsDeferredKey("Margin"));
                Assert.IsType<Thickness>(resources["Margin"]);
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
