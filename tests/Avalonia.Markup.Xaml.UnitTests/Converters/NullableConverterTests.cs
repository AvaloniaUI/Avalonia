using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Converters
{
    public class ClassWithNullableProperties
    {
        public Thickness? Thickness { get; set; }
        public Orientation? Orientation { get; set; }
    }
    
    public class NullableConverterTests : XamlTestBase
    {
        [Fact]
        public void Nullable_Types_Should_Still_Be_Converted_Properly()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformWrapper))
            {
                var xaml = @"<ClassWithNullableProperties 
xmlns='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Converters'
    Thickness = '5' Orientation='Vertical'
></ClassWithNullableProperties>";
                var loader = new AvaloniaXamlLoader();
                var data = (ClassWithNullableProperties)loader.Load(xaml, typeof(ClassWithNullableProperties).Assembly);
                Assert.Equal(new Thickness(5), data.Thickness);
                Assert.Equal(Orientation.Vertical, data.Orientation);
            }                
        }
    }
}
