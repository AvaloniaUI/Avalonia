// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Moq;
using OmniXaml;
using OmniXaml.ObjectAssembler.Commands;
using OmniXaml.TypeConversion;
using OmniXaml.Typing;
using Perspex.Markup.Xaml.Converters;
using Perspex.Styling;
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
            var target = new PerspexPropertyTypeConverter();
            var context = CreateContext();
            var result = target.ConvertFrom(context, null, "Class1.Foo");

            Assert.Equal(Class1.FooProperty, result);
        }

        [Fact]
        public void ConvertFrom_Uses_Selector_TargetType()
        {
            var target = new PerspexPropertyTypeConverter();
            var style = new Style(x => x.OfType<Class1>());
            var context = CreateContext(style);
            var result = target.ConvertFrom(context, null, "Foo");

            Assert.Equal(Class1.FooProperty, result);
        }

        [Fact]
        public void ConvertFrom_Finds_Attached_Property()
        {
            var target = new PerspexPropertyTypeConverter();
            var context = CreateContext();
            var result = target.ConvertFrom(context, null, "AttachedOwner.Attached");

            Assert.Equal(AttachedOwner.AttachedProperty, result);
        }

        private IXamlTypeConverterContext CreateContext(Style style = null)
        {
            var context = new Mock<IXamlTypeConverterContext>();
            var topDownValueContext = new Mock<ITopDownValueContext>();
            var typeRepository = new Mock<IXamlTypeRepository>();
            var featureProvider = new Mock<ITypeFeatureProvider>();
            var class1XamlType = new XamlType(typeof(Class1), typeRepository.Object, null, featureProvider.Object);
            var attachedOwnerXamlType = new XamlType(typeof(AttachedOwner), typeRepository.Object, null, featureProvider.Object);
            context.Setup(x => x.TopDownValueContext).Returns(topDownValueContext.Object);
            context.Setup(x => x.TypeRepository).Returns(typeRepository.Object);
            topDownValueContext.Setup(x => x.GetLastInstance(It.IsAny<XamlType>())).Returns(style);
            typeRepository.Setup(x => x.GetByQualifiedName("Class1")).Returns(class1XamlType);
            typeRepository.Setup(x => x.GetByQualifiedName("AttachedOwner")).Returns(attachedOwnerXamlType);
            return context.Object;
        }

        private class Class1 : PerspexObject, IStyleable
        {
            public static readonly PerspexProperty<string> FooProperty =
                PerspexProperty.Register<Class1, string>("Foo");

            public Classes Classes
            {
                get { throw new NotImplementedException(); }
            }

            public string Name
            {
                get { throw new NotImplementedException(); }
            }

            public Type StyleKey
            {
                get { throw new NotImplementedException(); }
            }

            public ITemplatedControl TemplatedParent
            {
                get { throw new NotImplementedException(); }
            }
        }

        private class AttachedOwner
        {
            public static readonly PerspexProperty<string> AttachedProperty =
                PerspexProperty.RegisterAttached<AttachedOwner, Class1, string>("Attached");
        }
    }
}
