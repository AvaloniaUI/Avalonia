// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Moq;
using Perspex.Controls;
using Perspex.Data;
using Perspex.Markup.Data;
using Perspex.Markup.Xaml.Data;
using ReactiveUI;
using Xunit;

namespace Perspex.Markup.Xaml.UnitTests.Data
{
    public class BindingTests
    {
        [Fact]
        public void OneWay_Binding_Should_Be_Set_Up()
        {
            var source = new Source { Foo = "foo" };
            var target = new TextBlock { DataContext = source };
            var binding = new Binding
            {
                Path = "Foo",
                Mode = BindingMode.OneWay,
            };

            target.Bind(TextBox.TextProperty, binding);

            Assert.Equal("foo", target.Text);
            source.Foo = "bar";
            Assert.Equal("bar", target.Text);
            target.Text = "baz";
            Assert.Equal("bar", source.Foo);
        }

        [Fact]
        public void TwoWay_Binding_Should_Be_Set_Up()
        {
            var source = new Source { Foo = "foo" };
            var target = new TextBlock { DataContext = source };
            var binding = new Binding
            {
                Path = "Foo",
                Mode = BindingMode.TwoWay,
            };

            target.Bind(TextBox.TextProperty, binding);

            Assert.Equal("foo", target.Text);
            source.Foo = "bar";
            Assert.Equal("bar", target.Text);
            target.Text = "baz";
            Assert.Equal("baz", source.Foo);
        }

        [Fact]
        public void OneTime_Binding_Should_Be_Set_Up()
        {
            var source = new Source { Foo = "foo" };
            var target = new TextBlock { DataContext = source };
            var binding = new Binding
            {
                Path = "Foo",
                Mode = BindingMode.OneTime,
            };

            target.Bind(TextBox.TextProperty, binding);

            Assert.Equal("foo", target.Text);
            source.Foo = "bar";
            Assert.Equal("foo", target.Text);
            target.Text = "baz";
            Assert.Equal("bar", source.Foo);
        }

        [Fact]
        public void OneWayToSource_Binding_Should_Be_Set_Up()
        {
            var source = new Source { Foo = "foo" };
            var target = new TextBlock { DataContext = source, Text = "bar" };
            var binding = new Binding
            {
                Path = "Foo",
                Mode = BindingMode.OneWayToSource,
            };

            target.Bind(TextBox.TextProperty, binding);

            Assert.Equal("bar", source.Foo);
            target.Text = "baz";
            Assert.Equal("baz", source.Foo);
            source.Foo = "quz";
            Assert.Equal("baz", target.Text);
        }

        [Fact]
        public void Default_BindingMode_Should_Be_Used()
        {
            var source = new Source { Foo = "foo" };
            var target = new TwoWayBindingTest { DataContext = source };
            var binding = new Binding
            {
                Path = "Foo",
            };

            target.Bind(TwoWayBindingTest.TwoWayProperty, binding);

            Assert.Equal("foo", target.TwoWay);
            source.Foo = "bar";
            Assert.Equal("bar", target.TwoWay);
            target.TwoWay = "baz";
            Assert.Equal("baz", source.Foo);
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

            parent.Child.Bind(Control.DataContextProperty, binding);

            Assert.Equal("Foo", parent.Child.DataContext);

            parentDataContext = Mock.Of<IHeadered>(x => x.Header == (object)"Bar");
            parent.DataContext = parentDataContext;
            Assert.Equal("Bar", parent.Child.DataContext);
        }

        [Fact]
        public void DataContext_Binding_Should_Track_Parent()
        {
            var parent = new Decorator
            {
                DataContext = new { Foo = "foo" },
            };

            var child = new Control();

            var binding = new Binding
            {
                Path = "Foo",
            };

            child.Bind(Control.DataContextProperty, binding);

            Assert.Null(child.DataContext);
            parent.Child = child;
            Assert.Equal("foo", child.DataContext);
        }

        [Fact]
        public void Should_Use_DefaultValueConverter_When_No_Converter_Specified()
        {
            var target = new TextBlock(); ;
            var binding = new Binding
            {
                Path = "Foo",
            };

            var result = binding.CreateSubject(target, TextBox.TextProperty);

            Assert.IsType<DefaultValueConverter>(((ExpressionSubject)result).Converter);
        }

        [Fact]
        public void Should_Use_Supplied_Converter()
        {
            var target = new TextBlock();
            var converter = new Mock<IValueConverter>();
            var binding = new Binding
            {
                Converter = converter.Object,
                Path = "Foo",
            };

            var result = binding.CreateSubject(target, TextBox.TextProperty);

            Assert.Same(converter.Object, ((ExpressionSubject)result).Converter);
        }

        [Fact]
        public void Should_Pass_ConverterParameter_To_Supplied_Converter()
        {
            var target = new TextBlock();
            var converter = new Mock<IValueConverter>();
            var binding = new Binding
            {
                Converter = converter.Object,
                ConverterParameter = "foo",
                Path = "Bar",
            };

            var result = binding.CreateSubject(target, TextBox.TextProperty);

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
            target.Bind(OldDataContextTest.FooProperty, fooBinding);
            target.Bind(OldDataContextTest.BarProperty, barBinding);
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

        private class TwoWayBindingTest : Control
        {
            public static readonly StyledProperty<string> TwoWayProperty =
                PerspexProperty.Register<TwoWayBindingTest, string>(
                    "TwoWay", 
                    defaultBindingMode: BindingMode.TwoWay);

            public string TwoWay
            {
                get { return GetValue(TwoWayProperty); }
                set { SetValue(TwoWayProperty, value); }
            }
        }

        public class Source : ReactiveObject
        {
            private string _foo;

            public string Foo
            {
                get { return _foo; }
                set { this.RaiseAndSetIfChanged(ref _foo, value); }
            }
        }

        private class OldDataContextViewModel
        {
            public int Foo { get; set; } = 1;
            public int Bar { get; set; } = 2;
        }

        private class OldDataContextTest : Control
        {
            public static readonly StyledProperty<int> FooProperty =
                PerspexProperty.Register<OldDataContextTest, int>("Foo");

            public static readonly StyledProperty<int> BarProperty =
              PerspexProperty.Register<OldDataContextTest, int>("Bar");

            public OldDataContextTest()
            {
                Bind(BarProperty, this.GetObservable(FooProperty));
            }
        }
    }
}
