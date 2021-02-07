using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class IgnoredDirectivesTests : XamlTestBase
    {
        [Fact]
        public void Ignored_Directives_Should_Compile()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                const string xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:sys='clr-namespace:System;assembly=netstandard'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBlock x:Name='target' x:FieldModifier='Public' Text='Foo'/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = window.FindControl<TextBlock>("target");

                window.ApplyTemplate();
                target.ApplyTemplate();

                Assert.Equal("Foo", target.Text);
            }
        }
    }
}
