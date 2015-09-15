// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Moq;
using OmniXaml;
using OmniXaml.TypeConversion;
using OmniXaml.Typing;
using Perspex.Markup.Xaml.Converters;
using Xunit;

namespace Perspex.Markup.Xaml.UnitTests.Converters
{
    public class PerspexPropertyConverterTest
    {
        public PerspexPropertyConverterTest()
        {
            // Ensure properties are registered.
            var foo = Class1.FooProperty;
            var attached = AttachedOwner.AttachedProperty;
        }

        [Fact]
        public void ConvertFrom_Finds_Fully_Qualified_Property()
        {
            var target = new PerspexPropertyConverter();
            var context = CreateContext();
            var result = target.ConvertFrom(context, null, "Class1.Foo");
        }

        [Fact]
        public void ConvertFrom_Finds_Attached_Property()
        {
            var target = new PerspexPropertyConverter();
            var context = CreateContext();
            var result = target.ConvertFrom(context, null, "AttachedOwner.Attached");
        }

        private IXamlTypeConverterContext CreateContext()
        {
            var context = new Mock<IXamlTypeConverterContext>();
            var typeRepository = new Mock<IXamlTypeRepository>();
            var featureProvider = new Mock<ITypeFeatureProvider>();
            var class1XamlType = new XamlType(typeof(Class1), typeRepository.Object, null, featureProvider.Object);
            var attachedOwnerXamlType = new XamlType(typeof(AttachedOwner), typeRepository.Object, null, featureProvider.Object);
            context.Setup(x => x.TypeRepository).Returns(typeRepository.Object);
            typeRepository.Setup(x => x.GetByQualifiedName("Class1")).Returns(class1XamlType);
            typeRepository.Setup(x => x.GetByQualifiedName("AttachedOwner")).Returns(attachedOwnerXamlType);
            return context.Object;
        }

        private class Class1 : PerspexObject
        {
            public static readonly PerspexProperty<string> FooProperty =
                PerspexProperty.Register<Class1, string>("Foo");
        }

        private class AttachedOwner
        {
            public static readonly PerspexProperty<string> AttachedProperty =
                PerspexProperty.RegisterAttached<AttachedOwner, Class1, string>("Attached");
        }
    }
}
