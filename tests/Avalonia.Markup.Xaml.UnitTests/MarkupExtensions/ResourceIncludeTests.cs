using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.MakrupExtensions
{
    public class ResourceIncludeTests
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

                    var loader = new AvaloniaXamlLoader();
                    var userControl = (UserControl)loader.Load(xaml);
                    var border = userControl.FindControl<Border>("border");

                    var brush = (SolidColorBrush)border.Background;
                    Assert.Equal(0xff506070, brush.Color.ToUint32());
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
