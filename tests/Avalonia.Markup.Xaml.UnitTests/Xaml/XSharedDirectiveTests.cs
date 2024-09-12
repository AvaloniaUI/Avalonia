using Avalonia.Controls;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml;

public class XSharedDirectiveTests : XamlTestBase
{
    [Fact]
    public void Should_Create_New_Instance_Where_x_Share_Is_False()
    {
        using (UnitTestApplication.Start(TestServices.StyledWindow))
        {
            const string xaml = $$"""
                                <Window xmlns="https://github.com/avaloniaui"
                                        xmlns:sys="clr-namespace:System;assembly=netstandard"
                                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                                    <Window.Resources>
                                        <ColumnDefinitions x:Key="ImplicitSharedResource">
                                            <ColumnDefinition Width="150" />
                                            <ColumnDefinition Width="10" />
                                            <ColumnDefinition Width="Auto" />
                                         </ColumnDefinitions>
                                         <ColumnDefinitions x:Key="NotSharedResource"
                                                            x:Shared="false">
                                            <ColumnDefinition Width="150" />
                                            <ColumnDefinition Width="10" />
                                            <ColumnDefinition Width="Auto" />
                                         </ColumnDefinitions>
                                    </Window.Resources>
                                </Window>
                                """;
            var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
            window.ApplyTemplate();

            var implicitSharedInstance1 = window.FindResource("ImplicitSharedResource");
            Assert.NotNull(implicitSharedInstance1);
            var implicitSharedInstance2 = window.FindResource("ImplicitSharedResource");
            Assert.NotNull(implicitSharedInstance2);

            Assert.Same(implicitSharedInstance1, implicitSharedInstance2);

            var notSharedResource1 = window.FindResource("NotSharedResource");
            Assert.NotNull(notSharedResource1);

            var notSharedResource2 = window.FindResource("NotSharedResource");
            Assert.NotNull(notSharedResource2);

            Assert.NotSame(notSharedResource1, notSharedResource2);

            Assert.Equal(notSharedResource1.ToString(), notSharedResource2.ToString());
        }
    }
}
