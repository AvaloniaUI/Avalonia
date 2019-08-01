using System;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Converters
{
    public class ConverterTests : XamlTestBase
    {
        [Fact]
        public void Bug_2228_Relative_Uris_Should_Be_Correctly_Parsed()
        {
            var testClass = typeof(TestClassWithUri);
            var parsed = AvaloniaXamlLoader.Parse<TestClassWithUri>(
                $"<{testClass.Name} xmlns='clr-namespace:{testClass.Namespace}' Uri='/test'/>", testClass.Assembly);

            Assert.False(parsed.Uri.IsAbsoluteUri);
        }
    }
    
    public class TestClassWithUri
    {
        public Uri Uri { get; set; }
    }
}
