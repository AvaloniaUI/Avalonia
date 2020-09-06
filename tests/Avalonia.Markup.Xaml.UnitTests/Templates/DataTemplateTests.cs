using Avalonia.Markup.Xaml.Templates;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Templates
{
    public class DataTemplateTests
    {
        [Fact]
        public void DataTemplate_Should_Match_Data_Of_Type()
        {
            var target = new DataTemplate { DataType = typeof(Class1) };
            var data = new Class1();

            Assert.True(target.Match(data));
        }

        [Fact]
        public void DataTemplate_Should_Match_Data_Of_Derived_Type()
        {
            var target = new DataTemplate { DataType = typeof(Class1) };
            var data = new Class2();

            Assert.True(target.Match(data));
        }

        private class Class1 { }
        private class Class2 : Class1 { }
    }
}
