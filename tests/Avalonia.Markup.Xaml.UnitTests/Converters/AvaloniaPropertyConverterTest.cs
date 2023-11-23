using System.ComponentModel;
using Avalonia.Markup.Xaml.Converters;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Styling;
using Moq;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Converters
{
    public class AvaloniaPropertyConverterTest : XamlTestBase
    {
        public AvaloniaPropertyConverterTest()
        {
            // Ensure properties are registered.
            _ = Class1.FooProperty;
            _ = AttachedOwner.AttachedProperty;
        }

        [Fact]
        public void ConvertFrom_Finds_Fully_Qualified_Property()
        {
            var target = new AvaloniaPropertyTypeConverter();
            var style = new Style(x => x.OfType<Class1>());
            var context = CreateContext(style);
            var result = target.ConvertFrom(context, null, "Class1.Foo");

            Assert.Equal(Class1.FooProperty, result);
        }

        [Fact]
        public void ConvertFrom_Uses_Selector_TargetType()
        {
            var target = new AvaloniaPropertyTypeConverter();
            var style = new Style(x => x.OfType<Class1>());
            var context = CreateContext(style);
            var result = target.ConvertFrom(context, null, "Foo");

            Assert.Equal(Class1.FooProperty, result);
        }

        [Fact]
        public void ConvertFrom_Finds_Attached_Property()
        {
            var target = new AvaloniaPropertyTypeConverter();
            var style = new Style(x => x.OfType<Class1>());
            var context = CreateContext(style);
            var result = target.ConvertFrom(context, null, "AttachedOwner.Attached");

            Assert.Equal(AttachedOwner.AttachedProperty, result);
        }

        [Fact]
        public void ConvertFrom_Finds_Attached_Property_With_Parentheses()
        {
            var target = new AvaloniaPropertyTypeConverter();
            var style = new Style(x => x.OfType<Class1>());
            var context = CreateContext(style);
            var result = target.ConvertFrom(context, null, "(AttachedOwner.Attached)");

            Assert.Equal(AttachedOwner.AttachedProperty, result);
        }

        [Fact]
        public void ConvertFrom_Throws_For_Nonexistent_Property()
        {
            var target = new AvaloniaPropertyTypeConverter();
            var style = new Style(x => x.OfType<Class1>());
            var context = CreateContext(style);

            var ex = Assert.Throws<XamlLoadException>(() => target.ConvertFrom(context, null, "Nonexistent"));

            Assert.Equal("Could not find property 'Class1.Nonexistent'.", ex.Message);
        }

        [Fact]
        public void ConvertFrom_Throws_For_Nonexistent_Attached_Property()
        {
            var target = new AvaloniaPropertyTypeConverter();
            var style = new Style(x => x.OfType<Class1>());
            var context = CreateContext(style);

            var ex = Assert.Throws<XamlLoadException>(() => target.ConvertFrom(context, null, "AttachedOwner.NonExistent"));

            Assert.Equal("Could not find property 'AttachedOwner.NonExistent'.", ex.Message);
        }


        
        private ITypeDescriptorContext CreateContext(Style style = null)
        {
            var tdMock = new Mock<ITypeDescriptorContext>();
            var tr = new Mock<IXamlTypeResolver>();
            var ps = new Mock<IAvaloniaXamlIlParentStackProvider>();

            tdMock.Setup(d => d.GetService(typeof(IXamlTypeResolver)))
                .Returns(tr.Object);

            tdMock.Setup(d => d.GetService(typeof(IAvaloniaXamlIlParentStackProvider)))
                .Returns(ps.Object);

            ps.SetupGet(v => v.Parents)
                .Returns(new object[] {style});
            
            tr.Setup(v => v.Resolve(nameof(Class1)))
                .Returns(typeof(Class1));
            tr.Setup(v => v.Resolve(nameof(AttachedOwner)))
                .Returns(typeof(AttachedOwner));

            return tdMock.Object;
        }

        private class Class1 : StyledElement
        {
            public static readonly StyledProperty<string> FooProperty =
                AvaloniaProperty.Register<Class1, string>("Foo");
        }

        private class AttachedOwner
        {
            public static readonly AttachedProperty<string> AttachedProperty =
                AvaloniaProperty.RegisterAttached<AttachedOwner, Class1, string>("Attached");
        }
    }
}
