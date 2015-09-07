namespace Perspex.Xaml.Base.UnitTest
{
    using Moq;
    using Markup.Xaml.DataBinding;
    using OmniXaml.TypeConversion;
    using Xunit;

    public class XamlBindingTest
    {
        [Fact]
        public void TestNullDataContext()
        {
            var t = new Mock<ITypeConverterProvider>();
            var sut = new XamlBinding(t.Object);
            sut.BindToDataContext(null);
        }
    }
}
