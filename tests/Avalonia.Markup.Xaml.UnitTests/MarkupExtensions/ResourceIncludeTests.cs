using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.MarkupExtensions
{
    public class ResourceIncludeTests : XamlTestBase
    {
        public class StaticResourceExtensionTests : XamlTestBase
        {
            [Fact]
            public void ResourceInclude_Loads_ResourceDictionary()
            {
                var documents = new[]
                {
                    new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Resource.xaml"), @"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
</ResourceDictionary>"),
                    new RuntimeXamlLoaderDocument(@"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceInclude Source='avares://Tests/Resource.xaml'/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </UserControl.Resources>

    <Border Name='border' Background='{StaticResource brush}'/>
</UserControl>")
                };

                using (StartWithResources())
                {
                    var compiled = AvaloniaRuntimeXamlLoader.LoadGroup(documents);
                    var userControl = Assert.IsType<UserControl>(compiled[1]);
                    var border = userControl.FindControl<Border>("border");

                    var brush = (ISolidColorBrush)border.Background;
                    Assert.Equal(0xff506070, brush.Color.ToUint32());
                }
            }

            [Fact]
            public void Missing_ResourceKey_In_ResourceInclude_Does_Not_Cause_StackOverflow()
            {
                var app = Application.Current;
                var documents = new[]
                {
                    new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Resource.xaml"), @"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <StaticResource x:Key='brush' ResourceKey='missing' />
</ResourceDictionary>"),
                    new RuntimeXamlLoaderDocument(app, @"
<Application xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceInclude Source='avares://Tests/Resource.xaml'/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>")
                };

                using (StartWithResources())
                {
                    try
                    {
                        AvaloniaRuntimeXamlLoader.LoadGroup(documents);
                    }
                    catch (KeyNotFoundException)
                    {

                    }
                }
            }

            private IDisposable StartWithResources(params (string, string)[] assets)
            {
                var assetLoader = new MockAssetLoader(assets);
                var services = new TestServices(assetLoader: assetLoader);
                return UnitTestApplication.Start(services);
            }
        }
    }
}
