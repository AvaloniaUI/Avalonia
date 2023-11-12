using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml;

public class StyleIncludeTests
{
    static StyleIncludeTests()
    {
        RuntimeHelpers.RunClassConstructor(typeof(RelativeSource).TypeHandle);
        AssetLoader.RegisterResUriParsers();
    }

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
    
    [Fact]
    public void Style_Inside_Resources_Should_Produce_Warning()
    {
        var diagnostics = new List<RuntimeXamlDiagnostic>();
        var control = (ContentControl)AvaloniaRuntimeXamlLoader.Load(new RuntimeXamlLoaderDocument(@"
<ContentControl xmlns='https://github.com/avaloniaui'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                xmlns:themes='clr-namespace:Avalonia.Themes.Simple;assembly=Avalonia.Themes.Simple'>
    <ContentControl.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <themes:SimpleTheme />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </ContentControl.Resources>
</ContentControl>"), new RuntimeXamlLoaderConfiguration
        {
            DiagnosticHandler = diagnostic =>
            {
                diagnostics.Add(diagnostic);
                return diagnostic.Severity;
            } 
        });
        Assert.IsAssignableFrom<IStyle>(((ResourceDictionary)control.Resources)!.MergedDictionaries[0]);
        var warning = Assert.Single(diagnostics);
        Assert.Equal(RuntimeXamlDiagnosticSeverity.Warning, warning.Severity);
    }

    [Fact]
    public void StyleInclude_From_CodeBehind_Resolves_Compiled()
    {
        using var locatorScope = AvaloniaLocator.EnterScope();
        AvaloniaLocator.CurrentMutable.BindToSelf<IAssetLoader>(new StandardAssetLoader(GetType().Assembly));
        
        var sp = new TestServiceProvider();
        var styleInclude = new StyleInclude(sp)
        {
            Source = new Uri("avares://Avalonia.Markup.Xaml.UnitTests/Xaml/StyleWithServiceProvider.xaml")
        };

        var loaded = Assert.IsType<StyleWithServiceProvider>(styleInclude.Loaded);
        
        Assert.Equal(
            sp.GetService<IAvaloniaXamlIlParentStackProvider>().Parents, 
            loaded.ServiceProvider.GetService<IAvaloniaXamlIlParentStackProvider>().Parents);
    }
}

public class TestServiceProvider : IServiceProvider, IUriContext, IAvaloniaXamlIlParentStackProvider
{
    private IServiceProvider _root = XamlIlRuntimeHelpers.CreateRootServiceProviderV2();
    public object GetService(Type serviceType)
    {
        if (serviceType == typeof(IUriContext))
        {
            return this;
        }
        if (serviceType == typeof(IAvaloniaXamlIlParentStackProvider))
        {
            return this;
        }
        return _root.GetService(serviceType);
    }

    public Uri BaseUri { get; set; }
    public List<object> Parents { get; set; } = new List<object> { new ContentControl() };
    IEnumerable<object> IAvaloniaXamlIlParentStackProvider.Parents => Parents;
}
