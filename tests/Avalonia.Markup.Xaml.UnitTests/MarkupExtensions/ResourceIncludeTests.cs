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
                var includeXaml = @"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
</ResourceDictionary>
";
                using (StartWithResources(("test:include.xaml", includeXaml)))
                {
                    var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceInclude Source='test:include.xaml'/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </UserControl.Resources>

    <Border Name='border' Background='{StaticResource brush}'/>
</UserControl>";

                    var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
                    var border = userControl.FindControl<Border>("border");

                    var brush = (ISolidColorBrush)border.Background;
                    Assert.Equal(0xff506070, brush.Color.ToUint32());
                }
            }

            [Fact]
            public void Missing_ResourceKey_In_ResourceInclude_Does_Not_Cause_StackOverflow()
            {
                var styleXaml = @"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <StaticResource x:Key='brush' ResourceKey='missing' />
</ResourceDictionary>";

                using (StartWithResources(("test:style.xaml", styleXaml)))
                {
                    var xaml = @"
<Application xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceInclude Source='test:style.xaml'/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>";

                    var app = Application.Current;

                    try
                    {
                        AvaloniaRuntimeXamlLoader.Load(xaml, null, app);
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
