// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Moq;
using Perspex.Controls;
using Perspex.Markup.Data;
using Perspex.Markup.Xaml.Data;
using Xunit;

namespace Perspex.Markup.Xaml.UnitTests.Data
{
    public class BindingTests
    {
        [Fact]
        public void OneWay_Binding_Should_Be_Set_Up()
        {
            var target = CreateTarget();
            var binding = new Binding
            {
                Path = "Foo",
                Mode = BindingMode.OneWay,
            };

            binding.Bind(target.Object, TextBox.TextProperty);

            target.Verify(x => x.Bind(
                TextBox.TextProperty, 
                It.IsAny<IObservable<object>>(), 
                BindingPriority.LocalValue));
        }

        [Fact]
        public void TwoWay_Binding_Should_Be_Set_Up()
        {
            var target = CreateTarget();
            var binding = new Binding
            {
                Path = "Foo",
                Mode = BindingMode.TwoWay,
            };

            binding.Bind(target.Object, TextBox.TextProperty);

            target.Verify(x => x.BindTwoWay(
                TextBox.TextProperty,
                It.IsAny<ISubject<object>>(),
                BindingPriority.LocalValue));
        }

        [Fact]
        public void OneTime_Binding_Should_Be_Set_Up()
        {
            var dataContext = new BehaviorSubject<object>(null);
            var expression = new BehaviorSubject<object>(null);
            var target = CreateTarget(dataContext: dataContext);
            var binding = new Binding
            {
                Path = "Foo",
                Mode = BindingMode.OneTime,
            };

            binding.Bind(target.Object, TextBox.TextProperty, expression);

            target.Verify(x => x.SetValue(
                (PerspexProperty)TextBox.TextProperty, 
                null, 
                BindingPriority.LocalValue));
            target.ResetCalls();

            expression.OnNext("foo");
            dataContext.OnNext(1);

            target.Verify(x => x.SetValue(
                (PerspexProperty)TextBox.TextProperty,
                "foo",
                BindingPriority.LocalValue));
        }

        [Fact]
        public void OneWayToSource_Binding_Should_Be_Set_Up()
        {
            var textObservable = new Mock<IObservable<string>>();
            var expression = new Mock<ISubject<object>>();
            var target = CreateTarget(text: textObservable.Object);
            var binding = new Binding
            {
                Path = "Foo",
                Mode = BindingMode.OneWayToSource,
            };

            binding.Bind(target.Object, TextBox.TextProperty, expression.Object);

            textObservable.Verify(x => x.Subscribe(expression.Object));
        }

        [Fact]
        public void Default_BindingMode_Should_Be_Used()
        {
            var target = CreateTarget(null);
            var binding = new Binding
            {
                Path = "Foo",
            };

            binding.Bind(target.Object, TextBox.TextProperty);

            // Default for TextBox.Text is two-way.
            target.Verify(x => x.BindTwoWay(
                TextBox.TextProperty,
                It.IsAny<ISubject<object>>(),
                BindingPriority.LocalValue));
        }

        [Fact]
        public void DataContext_Binding_Should_Use_Parent_DataContext()
        {
            var parentDataContext = Mock.Of<IHeadered>(x => x.Header == (object)"Foo");

            var parent = new Decorator
            {
                Child = new Control(),
                DataContext = parentDataContext,
            };

            var binding = new Binding
            {
                Path = "Header",
            };

            binding.Bind(parent.Child, Control.DataContextProperty);

            Assert.Equal("Foo", parent.Child.DataContext);

            parentDataContext = Mock.Of<IHeadered>(x => x.Header == (object)"Bar");
            parent.DataContext = parentDataContext;
            Assert.Equal("Bar", parent.Child.DataContext);
        }

        [Fact]
        public void Should_Use_DefaultValueConverter_When_No_Converter_Specified()
        {
            var target = CreateTarget(null);
            var binding = new Binding
            {
                Path = "Foo",
            };

            var result = binding.CreateSubject(target.Object, TextBox.TextProperty.PropertyType);

            Assert.IsType<DefaultValueConverter>(((ExpressionSubject)result).Converter);
        }

        [Fact]
        public void Should_Use_Supplied_Converter()
        {
            var target = CreateTarget(null);
            var converter = new Mock<IValueConverter>();
            var binding = new Binding
            {
                Converter = converter.Object,
                Path = "Foo",
            };

            var result = binding.CreateSubject(target.Object, TextBox.TextProperty.PropertyType);

            Assert.Same(converter.Object, ((ExpressionSubject)result).Converter);
        }

        [Fact]
        public void Should_Pass_ConverterParameter_To_Supplied_Converter()
        {
            var target = CreateTarget();
            var converter = new Mock<IValueConverter>();
            var binding = new Binding
            {
                Converter = converter.Object,
                ConverterParameter = "foo",
                Path = "Bar",
            };

            var result = binding.CreateSubject(target.Object, TextBox.TextProperty.PropertyType);

            Assert.Same("foo", ((ExpressionSubject)result).ConverterParameter);
        }

        /// <summary>
        /// Tests a problem discovered with ListBox with selection.
        /// </summary>
        /// <remarks>
        /// - Items is bound to DataContext first, followed by say SelectedIndex
        /// - When the ListBox is removed from the visual tree, DataContext becomes null (as it's
        ///   inherited)
        /// - This changes Items to null, which changes SelectedIndex to null as there are no
        ///   longer any items
        /// - However, the news that DataContext is now null hasn't yet reached the SelectedIndex
        ///   binding and so the unselection is sent back to the ViewModel
        /// </remarks>
        [Fact]
        public void Should_Not_Write_To_Old_DataContext()
        {
            var vm = new OldDataContextViewModel();
            var target = new OldDataContextTest();

            var fooBinding = new Binding
            {
                Path = "Foo",
                Mode = BindingMode.TwoWay,
            };

            var barBinding = new Binding
            {
                Path = "Bar",
                Mode = BindingMode.TwoWay,
            };

            // Bind Foo and Bar to the VM.
            fooBinding.Bind(target, OldDataContextTest.FooProperty);
            barBinding.Bind(target, OldDataContextTest.BarProperty);
            target.DataContext = vm;

            // Make sure the control's Foo and Bar properties are read from the VM
            Assert.Equal(1, target.GetValue(OldDataContextTest.FooProperty));
            Assert.Equal(2, target.GetValue(OldDataContextTest.BarProperty));

            // Set DataContext to null.
            target.DataContext = null;

            // Foo and Bar are no longer bound so they return 0, their default value.
            Assert.Equal(0, target.GetValue(OldDataContextTest.FooProperty));
            Assert.Equal(0, target.GetValue(OldDataContextTest.BarProperty));

            // The problem was here - DataContext is now null, setting Foo to 0. Bar is bound to 
            // Foo so Bar also gets set to 0. However the Bar binding still had a reference to
            // the VM and so vm.Bar was set to 0 erroneously.
            Assert.Equal(1, vm.Foo);
            Assert.Equal(2, vm.Bar);
        }

        private Mock<IObservablePropertyBag> CreateTarget(object dataContext)
        {
            return CreateTarget(dataContext: Observable.Never<object>().StartWith(dataContext));
        }

        private Mock<IObservablePropertyBag> CreateTarget(
            IObservable<object> dataContext = null,
            IObservable<string> text = null)
        {
            var result = new Mock<IObservablePropertyBag>();

            dataContext = dataContext ?? Observable.Never<object>().StartWith((object)null);
            text = text ?? Observable.Never<string>().StartWith((string)null);

            result.Setup(x => x.GetObservable(Control.DataContextProperty)).Returns(dataContext);
            result.Setup(x => x.GetObservable((PerspexProperty)Control.DataContextProperty)).Returns(dataContext);
            result.Setup(x => x.GetObservable((PerspexProperty)TextBox.TextProperty)).Returns(text);
            return result;
        }

        private class OldDataContextViewModel
        {
            public int Foo { get; set; } = 1;
            public int Bar { get; set; } = 2;
        }

        private class OldDataContextTest : Control
        {
            public static readonly PerspexProperty<int> FooProperty =
                PerspexProperty.Register<OldDataContextTest, int>("Foo");

            public static readonly PerspexProperty<int> BarProperty =
              PerspexProperty.Register<OldDataContextTest, int>("Bar");

            public OldDataContextTest()
            {
                Bind(BarProperty, GetObservable(FooProperty));
            }
        }
    }
}
