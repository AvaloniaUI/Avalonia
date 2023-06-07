using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Xml;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Styling;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml;

public class MergeResourceIncludeTests
{
    static MergeResourceIncludeTests()
    {
        RuntimeHelpers.RunClassConstructor(typeof(RelativeSource).TypeHandle);
    }

    [Fact]
    public void MergeResourceInclude_Works_With_Single_Resource()
    {
        var documents = new[]
        {
            new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Resources.xaml"), @"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <SolidColorBrush x:Key='brush2'>Red</SolidColorBrush>
</ResourceDictionary>"),
            new RuntimeXamlLoaderDocument(@"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <ResourceDictionary>
            <SolidColorBrush x:Key='brush1'>Blue</SolidColorBrush>
            <ResourceDictionary.MergedDictionaries>
                <MergeResourceInclude Source='avares://Tests/Resources.xaml'/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </UserControl.Resources>
</UserControl>")
        };
        
        var objects = AvaloniaRuntimeXamlLoader.LoadGroup(documents);
        var contentControl = Assert.IsType<UserControl>(objects[1]);

        var resources = Assert.IsType<ResourceDictionary>(contentControl.Resources);
        Assert.Empty(resources.MergedDictionaries);

        var initialResource = (ISolidColorBrush)resources["brush1"]!;
        Assert.Equal(Colors.Blue, initialResource.Color);

        var mergedResource = (ISolidColorBrush)resources["brush2"]!;
        Assert.Equal(Colors.Red, mergedResource.Color);
    }
    
    [Fact]
    public void Mixing_MergeResourceInclude_And_ResourceInclude_Is_Not_Allowed()
    {
        var documents = new[]
        {
            new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Resources1.xaml"), @"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <SolidColorBrush x:Key='brush1'>Red</SolidColorBrush>
</ResourceDictionary>"),
            new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Resources2.xaml"), @"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <SolidColorBrush x:Key='brush2'>Blue</SolidColorBrush>
</ResourceDictionary>"),
            new RuntimeXamlLoaderDocument(@"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ResourceDictionary.MergedDictionaries>
        <MergeResourceInclude Source='avares://Tests/Resources1.xaml'/>
        <ResourceInclude Source='avares://Tests/Resources2.xaml'/>
    </ResourceDictionary.MergedDictionaries>
</ResourceDictionary>")
        };
        
        Assert.ThrowsAny<XmlException>(() => AvaloniaRuntimeXamlLoader.LoadGroup(documents));
    }
    
    [Fact]
    public void MergeResourceInclude_Is_Allowed_After_ResourceInclude()
    {
        var documents = new[]
        {
            new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Resources1.xaml"), @"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <SolidColorBrush x:Key='brush1'>Red</SolidColorBrush>
</ResourceDictionary>"),
            new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Resources2.xaml"), @"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <SolidColorBrush x:Key='brush2'>Blue</SolidColorBrush>
</ResourceDictionary>"),
            new RuntimeXamlLoaderDocument(@"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ResourceDictionary.MergedDictionaries>
        <ResourceInclude Source='avares://Tests/Resources2.xaml'/>
        <MergeResourceInclude Source='avares://Tests/Resources1.xaml'/>
    </ResourceDictionary.MergedDictionaries>
</ResourceDictionary>")
        };
        
        AvaloniaRuntimeXamlLoader.LoadGroup(documents);
    }
    
    [Fact]
    public void MergeResourceInclude_Works_With_Multiple_Resources()
    {
        var documents = new[]
        {
            new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Resources1.xaml"), @"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <SolidColorBrush x:Key='brush1'>Red</SolidColorBrush>
    <SolidColorBrush x:Key='brush2'>Blue</SolidColorBrush>
</ResourceDictionary>"),
            new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Resources2.xaml"), @"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <SolidColorBrush x:Key='brush4'>Yellow</SolidColorBrush>
    <ResourceDictionary.MergedDictionaries>
        <MergeResourceInclude Source='avares://Tests/Resources1_2.xaml'/>
    </ResourceDictionary.MergedDictionaries>
</ResourceDictionary>"),
            new RuntimeXamlLoaderDocument(@"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ResourceDictionary.MergedDictionaries>
        <MergeResourceInclude Source='avares://Tests/Resources1.xaml'/>
        <MergeResourceInclude Source='avares://Tests/Resources2.xaml'/>
    </ResourceDictionary.MergedDictionaries>
    <SolidColorBrush x:Key='brush5'>Black</SolidColorBrush>
    <SolidColorBrush x:Key='brush6'>White</SolidColorBrush>
</ResourceDictionary>"),
            new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Resources1_2.xaml"), @"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <SolidColorBrush x:Key='brush3'>Green</SolidColorBrush>
</ResourceDictionary>"),
        };
        
        var objects = AvaloniaRuntimeXamlLoader.LoadGroup(documents);
        var resources = Assert.IsType<ResourceDictionary>(objects[2]);
        Assert.Empty(resources.MergedDictionaries);

        Assert.Equal(Colors.Red, ((ISolidColorBrush)resources["brush1"]!).Color);
        Assert.Equal(Colors.Blue, ((ISolidColorBrush)resources["brush2"]!).Color);
        Assert.Equal(Colors.Green, ((ISolidColorBrush)resources["brush3"]!).Color);
        Assert.Equal(Colors.Yellow, ((ISolidColorBrush)resources["brush4"]!).Color);
        Assert.Equal(Colors.Black, ((ISolidColorBrush)resources["brush5"]!).Color);
        Assert.Equal(Colors.White, ((ISolidColorBrush)resources["brush6"]!).Color);
    }

    [Fact]
    public void MergeResourceInclude_Works_With_ThemeDictionaries()
    {
        var documents = new[]
        {
            new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Resources1.xaml"), @"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ResourceDictionary.ThemeDictionaries>
        <ResourceDictionary x:Key='Light'>
            <SolidColorBrush x:Key='brush1'>White</SolidColorBrush>
            <SolidColorBrush x:Key='brush2'>Black</SolidColorBrush>
        </ResourceDictionary>
        <ResourceDictionary x:Key='Dark'>
            <SolidColorBrush x:Key='brush1'>Black</SolidColorBrush>
            <SolidColorBrush x:Key='brush2'>White</SolidColorBrush>
        </ResourceDictionary>
    </ResourceDictionary.ThemeDictionaries>
</ResourceDictionary>"),
            new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Resources2.xaml"), @"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ResourceDictionary.ThemeDictionaries>
        <ResourceDictionary x:Key='Light'>
            <SolidColorBrush x:Key='brush3'>Red</SolidColorBrush>
            <SolidColorBrush x:Key='brush4'>Blue</SolidColorBrush>
        </ResourceDictionary>
        <ResourceDictionary x:Key='Dark'>
            <SolidColorBrush x:Key='brush3'>Blue</SolidColorBrush>
            <SolidColorBrush x:Key='brush4'>Red</SolidColorBrush>
        </ResourceDictionary>
    </ResourceDictionary.ThemeDictionaries>
</ResourceDictionary>"),
            new RuntimeXamlLoaderDocument(@"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ResourceDictionary.MergedDictionaries>
        <MergeResourceInclude Source='avares://Tests/Resources1.xaml'/>
        <MergeResourceInclude Source='avares://Tests/Resources2.xaml'/>
    </ResourceDictionary.MergedDictionaries>
</ResourceDictionary>"),
        };
        
        var objects = AvaloniaRuntimeXamlLoader.LoadGroup(documents);
        var resources = Assert.IsType<ResourceDictionary>(objects[2]);
        Assert.Empty(resources.MergedDictionaries);

        Assert.Equal(Colors.White, Get("brush1", ThemeVariant.Light).Color);
        Assert.Equal(Colors.Black, Get("brush2", ThemeVariant.Light).Color);
        Assert.Equal(Colors.Black, Get("brush1", ThemeVariant.Dark).Color);
        Assert.Equal(Colors.White, Get("brush2", ThemeVariant.Dark).Color);

        Assert.Equal(Colors.Red, Get("brush3", ThemeVariant.Light).Color);
        Assert.Equal(Colors.Blue, Get("brush4", ThemeVariant.Light).Color);
        Assert.Equal(Colors.Blue, Get("brush3", ThemeVariant.Dark).Color);
        Assert.Equal(Colors.Red, Get("brush4", ThemeVariant.Dark).Color);

        ISolidColorBrush Get(string key, ThemeVariant themeVariant)
        {
            return resources.TryGetResource(key, themeVariant, out var res) ?
                (ISolidColorBrush)res! :
                throw new KeyNotFoundException();
        }
    }
    
        [Fact]
    public void MergeResourceInclude_Fails_With_ThemeDictionaries_Duplicate_Resources()
    {
        var documents = new[]
        {
            new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Resources1.xaml"), @"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ResourceDictionary.ThemeDictionaries>
        <ResourceDictionary x:Key='Light'>
            <SolidColorBrush x:Key='brush1'>White</SolidColorBrush>
        </ResourceDictionary>
    </ResourceDictionary.ThemeDictionaries>
</ResourceDictionary>"),
            new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Resources2.xaml"), @"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ResourceDictionary.ThemeDictionaries>
        <ResourceDictionary x:Key='Light'>
            <SolidColorBrush x:Key='brush1'>Black</SolidColorBrush>
        </ResourceDictionary>
    </ResourceDictionary.ThemeDictionaries>
</ResourceDictionary>"),
            new RuntimeXamlLoaderDocument(@"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ResourceDictionary.MergedDictionaries>
        <MergeResourceInclude Source='avares://Tests/Resources1.xaml'/>
        <MergeResourceInclude Source='avares://Tests/Resources2.xaml'/>
    </ResourceDictionary.MergedDictionaries>
</ResourceDictionary>"),
        };

        Assert.ThrowsAny<ArgumentException>(() => AvaloniaRuntimeXamlLoader.LoadGroup(documents));
    }
}
