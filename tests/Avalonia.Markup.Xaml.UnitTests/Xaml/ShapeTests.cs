using Avalonia.Controls;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class ShapeTests : XamlTestBase
    {
        [Fact]
        public void Can_Specify_DashStyle_In_XAML()
        {
            var xaml = @"
<Pen xmlns='https://github.com/avaloniaui'>
    <Pen.DashStyle>
	    <DashStyle Offset='0' Dashes='1,3'/>
    </Pen.DashStyle>
</Pen>";

            var target = AvaloniaRuntimeXamlLoader.Parse<Pen>(xaml);

            Assert.NotNull(target);
        }
    }
}
