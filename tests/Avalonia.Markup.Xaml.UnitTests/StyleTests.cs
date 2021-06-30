using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Data;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests
{
    public class StyleTests : XamlTestBase
    {
        [Fact]
        public void Binding_Should_Be_Assigned_To_Setter_Value_Instead_Of_Bound()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformWrapper))
            {
                var xaml = "<Style Selector='Button' xmlns='https://github.com/avaloniaui'><Setter Property='Content' Value='{Binding}'/></Style>";
                var style = (Style)AvaloniaRuntimeXamlLoader.Load(xaml);
                var setter = (Setter)(style.Setters.First());

                Assert.IsType<Binding>(setter.Value);
            }                
        }

        private class Data
        {
            public string Foo { get; set; }
        }
    }
}
