using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Data;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml;

public class ThemeDictionariesTests : XamlTestBase
{
    public static ThemeVariant Custom { get; } = new(nameof(Custom), ThemeVariant.Light);

    [Fact]
    public void DynamicResource_Updated_When_Control_Theme_Changed()
    {
        var themeVariantScope = (ThemeVariantScope)AvaloniaRuntimeXamlLoader.Load(@"
<ThemeVariantScope xmlns='https://github.com/avaloniaui'
              xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
              RequestedThemeVariant='Light'>
    <ThemeVariantScope.Resources>
        <ResourceDictionary>
            <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key='Dark'>
                    <SolidColorBrush x:Key='DemoBackground'>Black</SolidColorBrush>
                </ResourceDictionary>
                <ResourceDictionary x:Key='Light'>
                    <SolidColorBrush x:Key='DemoBackground'>White</SolidColorBrush>
                </ResourceDictionary>
            </ResourceDictionary.ThemeDictionaries>
        </ResourceDictionary>
    </ThemeVariantScope.Resources>

    <Border Name='border' Background='{DynamicResource DemoBackground}'/>
</ThemeVariantScope>");
        var border = (Border)themeVariantScope.Child!;
        
        Assert.Equal(Colors.White, ((ISolidColorBrush)border.Background)!.Color);

        themeVariantScope.RequestedThemeVariant = ThemeVariant.Dark;

        Assert.Equal(Colors.Black, ((ISolidColorBrush)border.Background)!.Color);
    }
    
    [Fact]
    public void DynamicResource_Updated_When_Control_Theme_Changed_No_Xaml()
    {
        var themeVariantScope = new ThemeVariantScope
        {
            RequestedThemeVariant = ThemeVariant.Light,
            Resources = new ResourceDictionary
            {
                ThemeDictionaries =
                {
                    [ThemeVariant.Dark] = new ResourceDictionary { ["DemoBackground"] = Brushes.Black },
                    [ThemeVariant.Light] = new ResourceDictionary { ["DemoBackground"] = Brushes.White }
                }
            },
            Child = new Border()
        };
        var border = (Border)themeVariantScope.Child!;
        border[!Border.BackgroundProperty] = new DynamicResourceExtension("DemoBackground");
        
        DelayedBinding.ApplyBindings(border);
        
        Assert.Equal(Colors.White, ((ISolidColorBrush)border.Background)!.Color);

        themeVariantScope.RequestedThemeVariant = ThemeVariant.Dark;

        Assert.Equal(Colors.Black, ((ISolidColorBrush)border.Background)!.Color);
    }

    [Fact]
    public void Intermediate_DynamicResource_Updated_When_Control_Theme_Changed()
    {
        var themeVariantScope = (ThemeVariantScope)AvaloniaRuntimeXamlLoader.Load(@"
<ThemeVariantScope xmlns='https://github.com/avaloniaui'
              xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
              RequestedThemeVariant='Light'>
    <ThemeVariantScope.Resources>
        <ResourceDictionary>
            <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key='Dark'>
                    <Color x:Key='TestColor'>Black</Color>
                </ResourceDictionary>
                <ResourceDictionary x:Key='Light'>
                    <Color x:Key='TestColor'>White</Color>
                </ResourceDictionary>
            </ResourceDictionary.ThemeDictionaries>
            <SolidColorBrush x:Key='DemoBackground' Color='{DynamicResource TestColor}' />
        </ResourceDictionary>
    </ThemeVariantScope.Resources>

    <Border Name='border' Background='{DynamicResource DemoBackground}'/>
</ThemeVariantScope>");
        var border = (Border)themeVariantScope.Child!;
        
        Assert.Equal(Colors.White, ((ISolidColorBrush)border.Background)!.Color);

        themeVariantScope.RequestedThemeVariant = ThemeVariant.Dark;

        Assert.Equal(Colors.Black, ((ISolidColorBrush)border.Background)!.Color);
    }

    [Fact]
    public void DynamicResource_In_ResourceProvider_Updated_When_Control_Theme_Changed()
    {
        var themeVariantScope = new ThemeVariantScope
        {
            RequestedThemeVariant = ThemeVariant.Light,
            Resources = new ResourceDictionary
            {
                ThemeDictionaries =
                {
                    [ThemeVariant.Dark] = new ResourceDictionary { ["DemoBackground"] = Brushes.Black },
                    [ThemeVariant.Light] = new ResourceDictionary { ["DemoBackground"] = Brushes.White }
                }
            },
            Child = new Border()
        };
        
        var resources = (ResourceDictionary)AvaloniaRuntimeXamlLoader.Load(@"
<ResourceDictionary xmlns='https://github.com/avaloniaui' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <GeometryDrawing x:Key='Geo' Brush='{DynamicResource DemoBackground}' />
</ResourceDictionary>");
        
        themeVariantScope.Resources.MergedDictionaries.Add(resources);
        var geo = (GeometryDrawing)themeVariantScope.FindResource("Geo");
        
        Assert.NotNull(geo);
        Assert.Equal(Colors.White, ((ISolidColorBrush)geo.Brush)!.Color);

        themeVariantScope.RequestedThemeVariant = ThemeVariant.Dark;
        
        Assert.Equal(Colors.Black, ((ISolidColorBrush)geo.Brush)!.Color);
    }

    [Fact]
    public void Intermediate_StaticResource_Can_Be_Reached_From_ThemeDictionaries()
    {
        var themeVariantScope = (ThemeVariantScope)AvaloniaRuntimeXamlLoader.Load(@"
<ThemeVariantScope xmlns='https://github.com/avaloniaui'
              xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
              RequestedThemeVariant='Light'>
    <ThemeVariantScope.Resources>
        <ResourceDictionary>
            <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key='Dark'>
                    <Color x:Key='TestColor'>Black</Color>
                    <StaticResource x:Key='DemoBackground' ResourceKey='TestColor' />
                </ResourceDictionary>
                <ResourceDictionary x:Key='Light'>
                    <Color x:Key='TestColor'>White</Color>
                    <StaticResource x:Key='DemoBackground' ResourceKey='TestColor' />
                </ResourceDictionary>
            </ResourceDictionary.ThemeDictionaries>
        </ResourceDictionary>
    </ThemeVariantScope.Resources>

    <Border Name='border' Background='{DynamicResource DemoBackground}'/>
</ThemeVariantScope>");
        var border = (Border)themeVariantScope.Child!;
        
        Assert.Equal(Colors.White, ((ISolidColorBrush)border.Background)!.Color);

        themeVariantScope.RequestedThemeVariant = ThemeVariant.Dark;

        Assert.Equal(Colors.Black, ((ISolidColorBrush)border.Background)!.Color);
    }

    [Fact]
    public void StaticResource_Inside_Of_ThemeDictionaries_Should_Use_Same_Theme_Key()
    {
        var themeVariantScope = (ThemeVariantScope)AvaloniaRuntimeXamlLoader.Load(@"
<ThemeVariantScope xmlns='https://github.com/avaloniaui'
              xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
              RequestedThemeVariant='Light'>
    <ThemeVariantScope.Resources>
        <ResourceDictionary>
            <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key='Dark'>
                    <Color x:Key='TestColor'>Black</Color>
                </ResourceDictionary>
                <ResourceDictionary x:Key='Light'>
                    <Color x:Key='TestColor'>White</Color>
                </ResourceDictionary>
            </ResourceDictionary.ThemeDictionaries>
        </ResourceDictionary>
    </ThemeVariantScope.Resources>

    <Border Name='border' Background='{DynamicResource DemoBackground}'>
        <Border.Resources>
            <ResourceDictionary>
                <ResourceDictionary.ThemeDictionaries>
                    <ResourceDictionary x:Key='Dark'>
                        <StaticResource x:Key='DemoBackground' ResourceKey='TestColor' />
                    </ResourceDictionary>
                    <ResourceDictionary x:Key='Light'>
                        <StaticResource x:Key='DemoBackground' ResourceKey='TestColor' />
                    </ResourceDictionary>
                </ResourceDictionary.ThemeDictionaries>
            </ResourceDictionary>
        </Border.Resources>
    </Border>
</ThemeVariantScope>");
        var border = (Border)themeVariantScope.Child!;
        
        Assert.Equal(Colors.White, ((ISolidColorBrush)border.Background)!.Color);

        themeVariantScope.RequestedThemeVariant = ThemeVariant.Dark;

        Assert.Equal(Colors.Black, ((ISolidColorBrush)border.Background)!.Color);
    }
    
    [Fact]
    public void StaticResource_Inside_Of_ThemeDictionaries_Should_Use_Same_Theme_Key_From_Inner_File()
    {
        var documents = new[]
        {
            new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Inner.xaml"), @"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <StaticResource x:Key='InnerKey' ResourceKey='OuterKey' />
</ResourceDictionary>"),
            new RuntimeXamlLoaderDocument(@"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ResourceDictionary.ThemeDictionaries>
        <ResourceDictionary x:Key='Default'>
            <Color x:Key='OuterKey'>Green</Color>
            <ResourceDictionary.MergedDictionaries>
                <ResourceInclude Source='avares://Tests/Inner.xaml'/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
        <ResourceDictionary x:Key='Dark'>
            <Color x:Key='OuterKey'>White</Color>
            <ResourceDictionary.MergedDictionaries>
                <ResourceInclude Source='avares://Tests/Inner.xaml'/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </ResourceDictionary.ThemeDictionaries>
</ResourceDictionary>")
        };
        
        var parsed = AvaloniaRuntimeXamlLoader.LoadGroup(documents);
        var dictionary = (ResourceDictionary)parsed[1]!;
        
        dictionary.TryGetResource("InnerKey", ThemeVariant.Dark, out var resource);
        var colorResource = Assert.IsType<Color>(resource);
        Assert.Equal(Colors.White, colorResource);
        
        dictionary.TryGetResource("InnerKey", ThemeVariant.Light, out resource);
        colorResource = Assert.IsType<Color>(resource);
        Assert.Equal(Colors.Green, colorResource);
    }
    
    [Fact]
    public void DynamicResource_Inside_Of_ThemeDictionaries_Should_Use_Same_Theme_Key_From_Inner_File()
    {
        var documents = new[]
        {
            new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Inner.xaml"), @"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <SolidColorBrush x:Key='InnerKey' Color='{DynamicResource OuterKey}' />
</ResourceDictionary>"),
            new RuntimeXamlLoaderDocument(@"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ResourceDictionary.ThemeDictionaries>
        <ResourceDictionary x:Key='Default'>
            <Color x:Key='OuterKey'>Green</Color>
            <ResourceDictionary.MergedDictionaries>
                <ResourceInclude Source='avares://Tests/Inner.xaml'/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
        <ResourceDictionary x:Key='Dark'>
            <Color x:Key='OuterKey'>White</Color>
            <ResourceDictionary.MergedDictionaries>
                <ResourceInclude Source='avares://Tests/Inner.xaml'/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </ResourceDictionary.ThemeDictionaries>
</ResourceDictionary>")
        };
        
        var parsed = AvaloniaRuntimeXamlLoader.LoadGroup(documents);
        var dictionary1 = (ResourceDictionary)parsed[0]!;
        var dictionary2 = (ResourceDictionary)parsed[1]!;
        var ownerApp = new Application(); // DynamicResource needs an owner to work
        ownerApp.RequestedThemeVariant = new ThemeVariant("FakeOne", null);
        ownerApp.Resources.MergedDictionaries.Add(dictionary1);
        ownerApp.Resources.MergedDictionaries.Add(dictionary2);
        
        dictionary2.TryGetResource("InnerKey", ThemeVariant.Dark, out var resource);
        var colorResource = Assert.IsAssignableFrom<ISolidColorBrush>(resource);
        Assert.Equal(Colors.White, colorResource.Color);
        
        dictionary2.TryGetResource("InnerKey", ThemeVariant.Light, out resource);
        colorResource = Assert.IsAssignableFrom<ISolidColorBrush>(resource);
        Assert.Equal(Colors.Green, colorResource.Color);
    }
    
    [Fact]
    public void DynamicResource_Inside_Control_Inside_Of_ThemeDictionaries_Should_Use_Control_Theme_Variant()
    {
        var documents = new[]
        {
            new RuntimeXamlLoaderDocument(@"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ResourceDictionary.ThemeDictionaries>
        <ResourceDictionary x:Key='Light'>
            <Color x:Key='ResourceKey'>Green</Color>
            <Template x:Key='Template'>
                <ThemeVariantScope RequestedThemeVariant='Dark' TextElement.Foreground='{DynamicResource ResourceKey}' />
            </Template>
        </ResourceDictionary>
        <ResourceDictionary x:Key='Dark'>
            <Color x:Key='ResourceKey'>White</Color>
            <Template x:Key='Template'>
                <ThemeVariantScope RequestedThemeVariant='Light' TextElement.Foreground='{DynamicResource ResourceKey}' />
            </Template>
        </ResourceDictionary>
    </ResourceDictionary.ThemeDictionaries>
</ResourceDictionary>")
        };
        
        var parsed = AvaloniaRuntimeXamlLoader.LoadGroup(documents);
        var dictionary = (ResourceDictionary)parsed[0]!;
        
        dictionary.TryGetResource("Template", ThemeVariant.Dark, out var resource);
        var control = Assert.IsType<ThemeVariantScope>((resource as Template)?.Build());
        control.Resources.MergedDictionaries.Add(dictionary);
        Assert.Equal(Colors.Green, ((ISolidColorBrush)control[TextElement.ForegroundProperty]!).Color);
        control.Resources.MergedDictionaries.Remove(dictionary);

        dictionary.TryGetResource("Template", ThemeVariant.Light, out resource);
        control = Assert.IsType<ThemeVariantScope>((resource as Template)?.Build());
        control.Resources.MergedDictionaries.Add(dictionary);
        Assert.Equal(Colors.White, ((ISolidColorBrush)control[TextElement.ForegroundProperty]!).Color);
    }

    [Fact]
    public void StaticResource_Outside_Of_Dictionaries_Should_Use_Control_ThemeVariant()
    {
        using (AvaloniaLocator.EnterScope())
        {
            var applicationThemeHost = new Mock<IThemeVariantHost>();
            applicationThemeHost.SetupGet(h => h.ActualThemeVariant).Returns(ThemeVariant.Dark);
            AvaloniaLocator.CurrentMutable.Bind<IThemeVariantHost>().ToConstant(applicationThemeHost.Object);

            var themeVariantScope = (ThemeVariantScope)AvaloniaRuntimeXamlLoader.Load(@"
<ThemeVariantScope xmlns='https://github.com/avaloniaui'
              xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
              RequestedThemeVariant='Light'>
    <ThemeVariantScope.Resources>
        <ResourceDictionary>
            <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key='Dark'>
                    <SolidColorBrush x:Key='DemoBackground'>Black</SolidColorBrush>
                </ResourceDictionary>
                <ResourceDictionary x:Key='Light'>
                    <SolidColorBrush x:Key='DemoBackground'>White</SolidColorBrush>
                </ResourceDictionary>
            </ResourceDictionary.ThemeDictionaries>
        </ResourceDictionary>
    </ThemeVariantScope.Resources>

    <Border Name='border' Background='{StaticResource DemoBackground}'/>
</ThemeVariantScope>");
            var border = (Border)themeVariantScope.Child!;

            themeVariantScope.RequestedThemeVariant = ThemeVariant.Light;
            Assert.Equal(Colors.White, ((ISolidColorBrush)border.Background)!.Color);
        }
    }
    
    [Fact]
    public void Inner_ThemeDictionaries_Works_Properly()
    {
        var themeVariantScope = (ThemeVariantScope)AvaloniaRuntimeXamlLoader.Load(@"
<ThemeVariantScope xmlns='https://github.com/avaloniaui'
              xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
              RequestedThemeVariant='Light'>
    <Border Name='border' Background='{DynamicResource DemoBackground}'>
        <Border.Resources>
            <ResourceDictionary>
                <ResourceDictionary.ThemeDictionaries>
                    <ResourceDictionary x:Key='Dark'>
                        <SolidColorBrush x:Key='DemoBackground'>Black</SolidColorBrush>
                    </ResourceDictionary>
                    <ResourceDictionary x:Key='Light'>
                        <SolidColorBrush x:Key='DemoBackground'>White</SolidColorBrush>
                    </ResourceDictionary>
                </ResourceDictionary.ThemeDictionaries>
            </ResourceDictionary>
        </Border.Resources>
    </Border>
</ThemeVariantScope>");
        var border = (Border)themeVariantScope.Child!;
        
        Assert.Equal(Colors.White, ((ISolidColorBrush)border.Background)!.Color);

        themeVariantScope.RequestedThemeVariant = ThemeVariant.Dark;

        Assert.Equal(Colors.Black, ((ISolidColorBrush)border.Background)!.Color);
    }
    
    [Fact]
    public void Inner_Resource_Can_Reference_Parent_ThemeDictionaries()
    {
        var themeVariantScope = (ThemeVariantScope)AvaloniaRuntimeXamlLoader.Load(@"
<ThemeVariantScope xmlns='https://github.com/avaloniaui'
              xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
              RequestedThemeVariant='Light'>
    <ThemeVariantScope.Resources>
        <ResourceDictionary>
            <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key='Dark'>
                    <Color x:Key='TestColor'>Black</Color>
                </ResourceDictionary>
                <ResourceDictionary x:Key='Light'>
                    <Color x:Key='TestColor'>White</Color>
                </ResourceDictionary>
            </ResourceDictionary.ThemeDictionaries>
        </ResourceDictionary>
    </ThemeVariantScope.Resources>

    <Border Name='border' Background='{DynamicResource DemoBackground}'>
        <Border.Resources>
            <ResourceDictionary>
                <SolidColorBrush x:Key='DemoBackground' Color='{DynamicResource TestColor}' />
            </ResourceDictionary>
        </Border.Resources>
    </Border>
</ThemeVariantScope>");
        var border = (Border)themeVariantScope.Child!;
        
        Assert.Equal(Colors.White, ((ISolidColorBrush)border.Background)!.Color);

        themeVariantScope.RequestedThemeVariant = ThemeVariant.Dark;

        Assert.Equal(Colors.Black, ((ISolidColorBrush)border.Background)!.Color);
    }
    
    [Fact]
    public void DynamicResource_Can_Access_Resources_Outside_Of_ThemeDictionaries()
    {
        var themeVariantScope = (ThemeVariantScope)AvaloniaRuntimeXamlLoader.Load(@"
<ThemeVariantScope xmlns='https://github.com/avaloniaui'
              xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
              RequestedThemeVariant='Light'>
    <ThemeVariantScope.Resources>
        <ResourceDictionary>
            <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key='Dark'>
                    <SolidColorBrush x:Key='DemoBackground' Color='{DynamicResource TestColor1}' />
                </ResourceDictionary>
                <ResourceDictionary x:Key='Light'>
                    <SolidColorBrush x:Key='DemoBackground' Color='{DynamicResource TestColor2}' />
                </ResourceDictionary>
            </ResourceDictionary.ThemeDictionaries>
            <Color x:Key='TestColor1'>Black</Color>
            <Color x:Key='TestColor2'>White</Color>
        </ResourceDictionary>
    </ThemeVariantScope.Resources>

    <Border Name='border' Background='{DynamicResource DemoBackground}' />
</ThemeVariantScope>");
        var border = (Border)themeVariantScope.Child!;
        
        Assert.Equal(Colors.White, ((ISolidColorBrush)border.Background)!.Color);

        themeVariantScope.RequestedThemeVariant = ThemeVariant.Dark;

        Assert.Equal(Colors.Black, ((ISolidColorBrush)border.Background)!.Color);
    }
    
    [Fact]
    public void Inner_Dictionary_Does_Not_Affect_Parent_Resources()
    {
        // It might be a nice feature, but neither Avalonia nor UWP supports it.
        // Better to expect this limitation with a unit test. 
        var themeVariantScope = (ThemeVariantScope)AvaloniaRuntimeXamlLoader.Load(@"
<ThemeVariantScope xmlns='https://github.com/avaloniaui'
              xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
              RequestedThemeVariant='Light'>
    <ThemeVariantScope.Resources>
        <ResourceDictionary>
            <Color x:Key='TestColor'>Red</Color>
            <SolidColorBrush x:Key='DemoBackground' Color='{DynamicResource TestColor}' />
        </ResourceDictionary>
    </ThemeVariantScope.Resources>

    <Border Name='border' Background='{DynamicResource DemoBackground}'>
        <Border.Resources>
            <ResourceDictionary>
                <ResourceDictionary.ThemeDictionaries>
                    <ResourceDictionary x:Key='Dark'>
                        <Color x:Key='TestColor'>Black</Color>
                    </ResourceDictionary>
                    <ResourceDictionary x:Key='Light'>
                        <Color x:Key='TestColor'>White</Color>
                    </ResourceDictionary>
                </ResourceDictionary.ThemeDictionaries>
            </ResourceDictionary>
        </Border.Resources>
    </Border>
</ThemeVariantScope>");
        var border = (Border)themeVariantScope.Child!;
        
        Assert.Equal(Colors.Red, ((ISolidColorBrush)border.Background)!.Color);

        themeVariantScope.RequestedThemeVariant = ThemeVariant.Dark;

        Assert.Equal(Colors.Red, ((ISolidColorBrush)border.Background)!.Color);
    }

    [Fact]
    public void Custom_Theme_Can_Be_Defined_In_ThemeDictionaries()
    {
        var themeVariantScope = (ThemeVariantScope)AvaloniaRuntimeXamlLoader.Load(@"
<ThemeVariantScope xmlns='https://github.com/avaloniaui'
              xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
              xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'
              RequestedThemeVariant='Light'>
    <ThemeVariantScope.Resources>
        <ResourceDictionary>
            <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key='Dark'>
                    <SolidColorBrush x:Key='DemoBackground'>Black</SolidColorBrush>
                </ResourceDictionary>
                <ResourceDictionary x:Key='Light'>
                    <SolidColorBrush x:Key='DemoBackground'>White</SolidColorBrush>
                </ResourceDictionary>
                <ResourceDictionary x:Key='{x:Static local:ThemeDictionariesTests.Custom}'>
                    <SolidColorBrush x:Key='DemoBackground'>Pink</SolidColorBrush>
                </ResourceDictionary>
            </ResourceDictionary.ThemeDictionaries>
        </ResourceDictionary>
    </ThemeVariantScope.Resources>

    <Border Name='border' Background='{DynamicResource DemoBackground}'/>
</ThemeVariantScope>");
        var border = (Border)themeVariantScope.Child!;

        themeVariantScope.RequestedThemeVariant = Custom;
        
        Assert.Equal(Colors.Pink, ((ISolidColorBrush)border.Background)!.Color);
    }
    
    [Fact]
    public void Custom_Theme_Fallbacks_To_Inherit_Theme_DynamicResource()
    {
        var themeVariantScope = (ThemeVariantScope)AvaloniaRuntimeXamlLoader.Load(@"
<ThemeVariantScope xmlns='https://github.com/avaloniaui'
              xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
              RequestedThemeVariant='Light'>
   <ThemeVariantScope.Resources>
        <ResourceDictionary>                
            <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key='Dark'>
                    <SolidColorBrush x:Key='DemoBackground'>Black</SolidColorBrush>
                </ResourceDictionary>
            </ResourceDictionary.ThemeDictionaries>
        </ResourceDictionary>
    </ThemeVariantScope.Resources> 
    <Border Background='{DynamicResource DemoBackground}' />
</ThemeVariantScope>");
        var border = (Border)themeVariantScope.Child!;
        
        themeVariantScope.RequestedThemeVariant = new ThemeVariant("Custom", ThemeVariant.Dark);

        Assert.Equal(Colors.Black, ((ISolidColorBrush)border.Background)!.Color);
    }
    
    [Fact]
    public void Custom_Theme_Fallbacks_To_Inherit_Theme_StaticResource()
    {
        var themeVariantScope = (ThemeVariantScope)AvaloniaRuntimeXamlLoader.Load(@"
<ThemeVariantScope xmlns='https://github.com/avaloniaui'
              xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ThemeVariantScope.RequestedThemeVariant>
        <ThemeVariant>
            <x:Arguments>
                <x:String>Custom</x:String>
                <ThemeVariant>Dark</ThemeVariant>
            </x:Arguments>
        </ThemeVariant>
    </ThemeVariantScope.RequestedThemeVariant>
   <ThemeVariantScope.Resources>
        <ResourceDictionary>                
            <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key='Dark'>
                    <SolidColorBrush x:Key='DemoBackground'>Black</SolidColorBrush>
                </ResourceDictionary>
            </ResourceDictionary.ThemeDictionaries>
        </ResourceDictionary>
    </ThemeVariantScope.Resources> 

    <Border Background='{StaticResource DemoBackground}' />
</ThemeVariantScope>");
        var border = (Border)themeVariantScope.Child!;

        Assert.Equal(Colors.Black, ((ISolidColorBrush)border.Background)!.Color);
    }
    
    [Fact]
    public void Theme_Switch_Works_In_Nested_Scope()
    {
        using (UnitTestApplication.Start(TestServices.StyledWindow))
        {
            var window = (Window)AvaloniaRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        RequestedThemeVariant='Dark'>
    <ThemeVariantScope Name='Scope'>
        <TextBlock Name='Text' />
    </ThemeVariantScope>
</Window>");
            window.ApplyTemplate();

            var scope = window.FindControl<ThemeVariantScope>("Scope")!;
            var text = window.FindControl<TextBlock>("Text")!;

            Assert.Equal(ThemeVariant.Dark, text.ActualThemeVariant);
            Assert.Equal(Color.Parse("#dedede"), ((ISolidColorBrush)text.Foreground!).Color);
            
            scope.RequestedThemeVariant = ThemeVariant.Light;
            Assert.Equal(ThemeVariant.Light, text.ActualThemeVariant);
            Assert.Equal(Colors.Black, ((ISolidColorBrush)text.Foreground!).Color);
        }
    }
    
    [Fact]
    public void Theme_Switch_Works_In_With_Popup()
    {
        using (UnitTestApplication.Start(TestServices.StyledWindow))
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ThemeVariantScope Name='Scope' RequestedThemeVariant='Dark'>
        <Popup Name='Popup'>
            <Border Width='100' Height='100'
                    Background='{DynamicResource ThemeBackgroundBrush}'>
            </Border>
        </Popup>
    </ThemeVariantScope>
</Window>");
                window.Show();

                var scope = window.FindControl<ThemeVariantScope>("Scope")!;
                var popup = window.FindControl<Popup>("Popup")!;
                
                popup.IsOpen = true;

                var border = (Border)popup.Child!;

                Assert.Equal(ThemeVariant.Dark, popup.ActualThemeVariant);
                Assert.Equal(ThemeVariant.Dark, border.ActualThemeVariant);
                Assert.Equal(Color.Parse("#282828"), ((ISolidColorBrush)border.Background!).Color);
                
                scope.RequestedThemeVariant = ThemeVariant.Light;

                Assert.Equal(ThemeVariant.Light, popup.ActualThemeVariant);
                Assert.Equal(ThemeVariant.Light, border.ActualThemeVariant);
                Assert.Equal(Colors.White, ((ISolidColorBrush)border.Background!).Color);
            }
        }
    }
}
