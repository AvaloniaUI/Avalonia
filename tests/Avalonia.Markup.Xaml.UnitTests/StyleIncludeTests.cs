using System;
using System.Collections.Generic;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests
{
    public class StyleIncludeTests : XamlTestBase
    {
        [Fact]
        public void Missing_ResourceKey_In_StyleInclude_Does_Not_Cause_StackOverflow()
        {
            var styleXaml = @"
<Style xmlns='https://github.com/avaloniaui'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Style.Resources>
        <StaticResource x:Key='brush' ResourceKey='missing' />
    </Style.Resources>
</Style>";

            using (StyleIncludeTests.StartWithResources(("test:style.xaml", styleXaml)))
            {
                var xaml = @"
<Application xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Application.Styles>
        <StyleInclude Source='test:style.xaml'/>
    </Application.Styles>
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

        private static IDisposable StartWithResources(params (string, string)[] assets)
        {
            var assetLoader = new MockAssetLoader(assets);
            var services = new TestServices(assetLoader: assetLoader);
            return UnitTestApplication.Start(services);
        }
    }
}
