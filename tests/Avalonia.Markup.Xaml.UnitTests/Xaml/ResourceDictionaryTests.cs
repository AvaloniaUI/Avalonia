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
            using (StyledWindow())
            {
                var documents = new[]
                {
                    new RuntimeXamlLoaderDocument(new Uri("avares://Avalonia.Markup.Xaml.UnitTests/dict.xaml"), @"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <SolidColorBrush x:Key='RedBrush' Color='{DynamicResource Red}'/>
</ResourceDictionary>"),
                    new RuntimeXamlLoaderDocument(@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceInclude Source='avares://Avalonia.Markup.Xaml.UnitTests/dict.xaml'/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
        <Color x:Key='Red'>Red</Color>
    </Window.Resources>
    <Button Name='button' Background='{DynamicResource RedBrush}'/>
</Window>")
                };

                var loaded = AvaloniaRuntimeXamlLoader.LoadGroup(documents);
                var window = Assert.IsType<Window>(loaded[1]);
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

        [Fact]
        public void Closest_Resource_Should_Be_Referenced()
        {
            using (StyledWindow())
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <SolidColorBrush x:Key='Red' Color='Red' />
        <StaticResource x:Key='Red2' ResourceKey='Red' />
    </Window.Resources>
    <Button>
        <Button.Resources>
            <SolidColorBrush x:Key='Red' Color='Blue' />
        </Button.Resources>
    </Button>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var windowResources = (ResourceDictionary)window.Resources;
                var buttonResources = (ResourceDictionary)((Button)window.Content!).Resources;
                
                var brush = Assert.IsType<SolidColorBrush>(windowResources["Red2"]);
                Assert.Equal(Colors.Red, brush.Color);
                
                Assert.False(windowResources.ContainsDeferredKey("Red"));
                Assert.False(windowResources.ContainsDeferredKey("Red2"));

                Assert.True(buttonResources.ContainsDeferredKey("Red"));
            }
        }
        
        [Fact]
        public void Should_Be_Possible_To_Redefine_Referenced_Resource_ControlTheme()
        {
            using (StyledWindow())
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <ControlTheme x:Key='{x:Type Button}' TargetType='Button' />
    </Window.Resources>
    <UserControl>
        <UserControl.Resources>
            <ControlTheme x:Key='{x:Type Button}' TargetType='Button' BasedOn='{StaticResource {x:Type Button}}' />
        </UserControl.Resources>
    </UserControl>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var windowResources = (ResourceDictionary)window.Resources;
                var innerResources = (ResourceDictionary)((UserControl)window.Content!).Resources;

                var winButtonTheme = Assert.IsType<ControlTheme>(windowResources[typeof(Button)]);
                var innerButtonTheme = Assert.IsType<ControlTheme>(innerResources[typeof(Button)]);
                Assert.Equal(winButtonTheme, innerButtonTheme.BasedOn);
            }
        }
        
        [Fact]
        public void Should_Be_Possible_To_Redefine_Referenced_Resource()
        {
            using (StyledWindow())
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <Color x:Key='SystemAccentColor'>#aaa</Color>
    </Window.Resources>
    <UserControl>
        <UserControl.Resources>
            <StaticResource x:Key='SystemAccentColor' ResourceKey='SystemAccentColor' />
        </UserControl.Resources>
    </UserControl>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var windowResources = (ResourceDictionary)window.Resources;
                var innerResources = (ResourceDictionary)((UserControl)window.Content!).Resources;

                var winButtonTheme = Assert.IsType<Color>(windowResources["SystemAccentColor"]);
                var innerButtonTheme = Assert.IsType<Color>(innerResources["SystemAccentColor"]);
                Assert.Equal(winButtonTheme, innerButtonTheme);
            }
        }

        [Fact]
        public void Dynamically_Changing_Referenced_Resources_Works_With_DynamicResource()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <UserControl.Resources>
    <Color x:Key='color'>Red</Color>
    <SolidColorBrush x:Key='brush' Color='{DynamicResource color}' />
  </UserControl.Resources>
</UserControl>";

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);

            Assert.Equal(Colors.Red, ((ISolidColorBrush)userControl.FindResource("brush")!).Color);

            userControl.Resources.Remove("color");
            Assert.Equal(default, ((ISolidColorBrush)userControl.FindResource("brush")!).Color);

            userControl.Resources.Add("color", Colors.Blue);
            Assert.Equal(Colors.Blue, ((ISolidColorBrush)userControl.FindResource("brush")!).Color);
        }

        [Fact]
        public void ResourceDictionary_Can_Be_Put_Inside_Of_ResourceDictionary()
        {
            using (StyledWindow())
            {
                var xaml = @"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ResourceDictionary x:Key='NotAThemeVariantKey' />
</ResourceDictionary>";
                var resources = (ResourceDictionary)AvaloniaRuntimeXamlLoader.Load(xaml);
                var nested = (ResourceDictionary)resources["NotAThemeVariantKey"];

                Assert.NotNull(nested);
            }
        }
        
        private IDisposable StyledWindow(params (string, string)[] assets)
        {
            var services = TestServices.StyledWindow.With(
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
