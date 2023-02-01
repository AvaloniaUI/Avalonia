using Avalonia.Controls;
using Avalonia.Markup.Data;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
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

    [Fact(Skip = "Not implemented")]
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
}
