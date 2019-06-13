// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Moq;
using Avalonia.Collections;
using Avalonia.Markup.Xaml.Converters;
using Avalonia.Styling;
using Xunit;
using System.ComponentModel;
using Avalonia.Markup.Xaml.XamlIl.Runtime;

namespace Avalonia.Markup.Xaml.UnitTests.Converters
{
    public class AvaloniaPropertyConverterTest
    {
        public AvaloniaPropertyConverterTest()
        {
            // Ensure properties are registered.
            var foo = Class1.FooProperty;
            var attached = AttachedOwner.AttachedProperty;
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

        private class Class1 : AvaloniaObject, IStyleable
        {
            public static readonly StyledProperty<string> FooProperty =
                AvaloniaProperty.Register<Class1, string>("Foo");

            public IAvaloniaReadOnlyList<string> Classes
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

            IObservable<IStyleable> IStyleable.StyleDetach { get; }
        }

        private class AttachedOwner
        {
            public static readonly AttachedProperty<string> AttachedProperty =
                AvaloniaProperty.RegisterAttached<AttachedOwner, Class1, string>("Attached");
        }
    }
}
