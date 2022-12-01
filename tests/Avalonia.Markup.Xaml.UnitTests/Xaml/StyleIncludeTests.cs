using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml;

public class StyleIncludeTests
{
    [Fact]
    public void StyleInclude_Is_Built()
    {
        using (UnitTestApplication.Start(TestServices.StyledWindow
                   .With(theme: () => new Styles())))
        {
            var xaml = @"
<ContentControl xmlns='https://github.com/avaloniaui'>
    <ContentControl.Styles>
        <StyleInclude Source='avares://Avalonia.Markup.Xaml.UnitTests/Xaml/Style1.xaml'/>
    </ContentControl.Styles>
</ContentControl>";

            var window = AvaloniaRuntimeXamlLoader.Parse<ContentControl>(xaml);
                
            Assert.IsType<Style>(window.Styles[0]);
        }
    }
        
    [Fact]
    public void StyleInclude_Is_Built_Resources()
    {
        using (UnitTestApplication.Start(TestServices.StyledWindow
                   .With(theme: () => new Styles())))
        {
            var xaml = @"
<ContentControl xmlns='https://github.com/avaloniaui'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ContentControl.Resources>
        <StyleInclude x:Key='Include' Source='avares://Avalonia.Markup.Xaml.UnitTests/Xaml/Style1.xaml'/>
    </ContentControl.Resources>
</ContentControl>";

            var contentControl = AvaloniaRuntimeXamlLoader.Parse<ContentControl>(xaml);

            Assert.IsType<Style>(contentControl.Resources["Include"]);
        }
    }

    [Fact]
    public void StyleInclude_Is_Resolved_With_Two_Files()
    {
        var documents = new[]
        {
            new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Style.xaml"), @"
<Style xmlns='https://github.com/avaloniaui'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Style.Resources>
        <Color x:Key='Red'>Red</Color>
    </Style.Resources>
</Style>"),
            new RuntimeXamlLoaderDocument(@"
<ContentControl xmlns='https://github.com/avaloniaui'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ContentControl.Resources>
        <StyleInclude x:Key='Include' Source='avares://Tests/Style.xaml'/>
    </ContentControl.Resources>
</ContentControl>")
        };
        
        var objects = AvaloniaRuntimeXamlLoader.LoadGroup(documents);
        var style = Assert.IsType<Style>(objects[0]);
        var contentControl = Assert.IsType<ContentControl>(objects[1]);

        Assert.IsType<Style>(contentControl.Resources["Include"]);
    }
    
    [Fact]
    public void Relative_Back_StyleInclude_Is_Resolved_With_Two_Files()
    {
        var documents = new[]
        {
            new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Subfolder/Style.xaml"), @"
<Style xmlns='https://github.com/avaloniaui'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Style.Resources>
        <Color x:Key='Red'>Red</Color>
    </Style.Resources>
</Style>"),
            new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Subfolder/Folder/Root.xaml"), @"
<ContentControl xmlns='https://github.com/avaloniaui'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ContentControl.Resources>
        <StyleInclude x:Key='Include' Source='../Style.xaml'/>
    </ContentControl.Resources>
</ContentControl>")
        };
        
        var objects = AvaloniaRuntimeXamlLoader.LoadGroup(documents);
        var style = Assert.IsType<Style>(objects[0]);
        var contentControl = Assert.IsType<ContentControl>(objects[1]);

        Assert.IsType<Style>(contentControl.Resources["Include"]);
    }
    
    [Fact]
    public void Relative_Root_StyleInclude_Is_Resolved_With_Two_Files()
    {
        var documents = new[]
        {
            new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Style.xaml"), @"
<Style xmlns='https://github.com/avaloniaui'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Style.Resources>
        <Color x:Key='Red'>Red</Color>
    </Style.Resources>
</Style>"),
            new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Folder/Root.xaml"), @"
<ContentControl xmlns='https://github.com/avaloniaui'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ContentControl.Resources>
        <StyleInclude x:Key='Include' Source='/Style.xaml'/>
    </ContentControl.Resources>
</ContentControl>")
        };
        
        var objects = AvaloniaRuntimeXamlLoader.LoadGroup(documents);
        var style = Assert.IsType<Style>(objects[0]);
        var contentControl = Assert.IsType<ContentControl>(objects[1]);

        Assert.IsType<Style>(contentControl.Resources["Include"]);
    }
    
    [Fact]
    public void Relative_StyleInclude_Is_Resolved_With_Two_Files()
    {
        var documents = new[]
        {
            new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Folder/Style.xaml"), @"
<Style xmlns='https://github.com/avaloniaui'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Style.Resources>
        <Color x:Key='Red'>Red</Color>
    </Style.Resources>
</Style>"),
            new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Folder/Root.xaml"), @"
<ContentControl xmlns='https://github.com/avaloniaui'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ContentControl.Resources>
        <StyleInclude x:Key='Include' Source='Style.xaml'/>
    </ContentControl.Resources>
</ContentControl>")
        };
        
        var objects = AvaloniaRuntimeXamlLoader.LoadGroup(documents);
        var style = Assert.IsType<Style>(objects[0]);
        var contentControl = Assert.IsType<ContentControl>(objects[1]);

        Assert.IsType<Style>(contentControl.Resources["Include"]);
    }
    
    [Fact]
    public void Relative_Dot_Syntax__StyleInclude_Is_Resolved_With_Two_Files()
    {
        var documents = new[]
        {
            new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Folder/Style.xaml"), @"
<Style xmlns='https://github.com/avaloniaui'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Style.Resources>
        <Color x:Key='Red'>Red</Color>
    </Style.Resources>
</Style>"),
            new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Folder/Root.xaml"), @"
<ContentControl xmlns='https://github.com/avaloniaui'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ContentControl.Resources>
        <StyleInclude x:Key='Include' Source='./Style.xaml'/>
    </ContentControl.Resources>
</ContentControl>")
        };
        
        var objects = AvaloniaRuntimeXamlLoader.LoadGroup(documents);
        var style = Assert.IsType<Style>(objects[0]);
        var contentControl = Assert.IsType<ContentControl>(objects[1]);

        Assert.IsType<Style>(contentControl.Resources["Include"]);
    }
    
    [Fact]
    public void NonLatin_StyleInclude_Is_Resolved_With_Two_Files()
    {
        var documents = new[]
        {
            new RuntimeXamlLoaderDocument(new Uri("avares://アセンブリ/スタイル.xaml"), @"
<Style xmlns='https://github.com/avaloniaui'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Style.Resources>
        <Color x:Key='Red'>Red</Color>
    </Style.Resources>
</Style>"),
            new RuntimeXamlLoaderDocument(@"
<ContentControl xmlns='https://github.com/avaloniaui'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ContentControl.Resources>
        <StyleInclude x:Key='Include' Source='avares://アセンブリ/スタイル.xaml'/>
    </ContentControl.Resources>
</ContentControl>")
        };
        
        var objects = AvaloniaRuntimeXamlLoader.LoadGroup(documents);
        var style = Assert.IsType<Style>(objects[0]);
        var contentControl = Assert.IsType<ContentControl>(objects[1]);

        Assert.IsType<Style>(contentControl.Resources["Include"]);
    }
    
    [Fact]
    public void Missing_ResourceKey_In_StyleInclude_Does_Not_Cause_StackOverflow()
    {
        var documents = new[]
        {
            new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Style.xaml"), @"
<Style xmlns='https://github.com/avaloniaui'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Style.Resources>
        <StaticResource x:Key='brush' ResourceKey='missing' />
    </Style.Resources>
</Style>"),
            new RuntimeXamlLoaderDocument(@"
<ContentControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ContentControl.Styles>
        <StyleInclude Source='avares://Tests/Style.xaml'/>
    </ContentControl.Styles>
</ContentControl>")
        };


        try
        {
            _ = AvaloniaRuntimeXamlLoader.LoadGroup(documents);
        }
        catch (KeyNotFoundException)
        {

        }
    }
    
    [Fact]
    public void StyleInclude_Should_Be_Replaced_With_Direct_Call()
    {
        var control = (ContentControl)AvaloniaRuntimeXamlLoader.Load(@"
<ContentControl xmlns='https://github.com/avaloniaui'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                xmlns:themes='clr-namespace:Avalonia.Themes.Simple;assembly=Avalonia.Themes.Simple'>
    <ContentControl.Styles>
        <themes:SimpleTheme />
        <StyleInclude Source='avares://Avalonia.Themes.Simple/SimpleTheme.xaml'/>
    </ContentControl.Styles>
</ContentControl>");
        Assert.IsType<SimpleTheme>(control.Styles[0]);
        Assert.IsType<SimpleTheme>(control.Styles[1]);
    }
}
