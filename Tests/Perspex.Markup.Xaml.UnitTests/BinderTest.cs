namespace Perspex.Xaml.Base.UnitTest
{
    using System;
    using Moq;
    using Markup.Xaml.DataBinding;
    using OmniXaml.TypeConversion;
    using Xunit;

    public class BinderTest
    {
        [Fact]        
        public void NullTarget_Throws()
        {
            var typeConverter = new Mock<ITypeConverterProvider>();
            var perspexPropertyBinder = new PerspexPropertyBinder(typeConverter.Object);
            var bindingDefinitionBuilder = new BindingDefinitionBuilder();
            var binding = bindingDefinitionBuilder
                .WithNullTarget()
                .Build();

            var exception = Assert.Throws<InvalidOperationException>(() => perspexPropertyBinder.Create(binding));
        }
    }
}
