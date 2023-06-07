using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Converters
{
    public class GeometryTypeConverterTests: XamlTestBase
    {
        public class StringDataViewModel
        {
            public string PathData { get; set; } 
        }

        public class IntDataViewModel
        {
            public int PathData { get; set; }
        }


        [Theory]
        [MemberData(nameof(Get_GeometryTypeConverter_Data))]
        public void GeometryTypeConverter_Value_Work(object vm, bool nullData)
        {
            using(UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:c='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Converters;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Path Name='path' Data='{Binding PathData}' Height='10' Width='10'/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var path = window.FindControl<Path>("path");
                window.DataContext = vm;
                Assert.Equal(nullData, path.Data is null);
            }
        }

        public static IEnumerable<object[]> Get_GeometryTypeConverter_Data()
        {
            yield return new object[] { new StringDataViewModel { }, true };
            yield return new object[] { new StringDataViewModel { PathData = "M406.39,333.45l205.93,0" }, false };
            yield return new object[] { new IntDataViewModel { }, true };
            yield return new object[] { new IntDataViewModel { PathData = 100 }, true };
        }
    }
}
