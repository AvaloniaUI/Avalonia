// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using System.Reactive.Subjects;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Moq;
using Xunit;

namespace Avalonia.Styling.UnitTests
{
    public class SetterTests
    {
        [Fact]
        public void Cannot_Assign_Control_To_Value()
        {
            var target = new Setter();

            Assert.Throws<InvalidOperationException>(() => target.Value = new Border());
        }

        [Fact]
        public void Setter_Should_Apply_Binding_To_Property()
        {
            var control = new TextBlock();
            var subject = new BehaviorSubject<object>("foo");
            var descriptor = InstancedBinding.OneWay(subject);
            var binding = Mock.Of<IBinding>(x => x.Initiate(control, TextBlock.TextProperty, null, false) == descriptor);
            var style = Mock.Of<IStyle>();
            var setter = new Setter(TextBlock.TextProperty, binding);

            setter.Apply(style, control, null);

            Assert.Equal("foo", control.Text);
        }

        [Fact]
        public void Setter_Should_Materialize_Template_To_Property()
        {
            var control = new Decorator();
            var template = new FuncTemplate<Canvas>(() => new Canvas());
            var style = Mock.Of<IStyle>();
            var setter = new Setter(Decorator.ChildProperty, template);

            setter.Apply(style, control, null);

            Assert.IsType<Canvas>(control.Child);
        }

        [Fact]
        public void Does_Not_Call_Converter_ConvertBack_On_OneWay_Binding()
        {
            var control = new Decorator { Name = "foo" };
            var style = Mock.Of<IStyle>();
            var binding = new Binding("Name", BindingMode.OneWay)
            {
                Converter = new TestConverter(),
                RelativeSource = new RelativeSource(RelativeSourceMode.Self),
            };
            var setter = new Setter(Decorator.TagProperty, binding);
            var activator = new BehaviorSubject<bool>(true);

            setter.Apply(style, control, activator);
            Assert.Equal("foobar", control.Tag);

            // Issue #1218 caused TestConverter.ConvertBack to throw here.
            activator.OnNext(false);
            Assert.Null(control.Tag);
        }

        [Fact]
        public void Setter_Should_Apply_Value_Without_Activator_With_Style_Priority()
        {
            var control = new Mock<IStyleable>();
            var style = Mock.Of<Style>();
            var setter = new Setter(TextBlock.TextProperty, "foo");

            setter.Apply(style, control.Object, null);

            control.Verify(x => x.Bind(
                TextBlock.TextProperty,
                It.IsAny<IObservable<object>>(),
                BindingPriority.Style));
        }

        [Fact]
        public void Setter_Should_Apply_Value_With_Activator_With_StyleTrigger_Priority()
        {
            var control = new Mock<IStyleable>();
            var style = Mock.Of<Style>();
            var setter = new Setter(TextBlock.TextProperty, "foo");
            var activator = new Subject<bool>();

            setter.Apply(style, control.Object, activator);

            control.Verify(x => x.Bind(
                TextBlock.TextProperty,
                It.IsAny<IObservable<object>>(),
                BindingPriority.StyleTrigger));
        }

        [Fact]
        public void Setter_Should_Apply_Binding_Without_Activator_With_Style_Priority()
        {
            var control = new Mock<IStyleable>();
            var style = Mock.Of<Style>();
            var setter = new Setter(TextBlock.TextProperty, CreateMockBinding(TextBlock.TextProperty));

            setter.Apply(style, control.Object, null);

            control.Verify(x => x.Bind(
                TextBlock.TextProperty,
                It.IsAny<IObservable<object>>(),
                BindingPriority.Style));
        }

        [Fact]
        public void Setter_Should_Apply_Binding_With_Activator_With_StyleTrigger_Priority()
        {
            var control = new Mock<IStyleable>();
            var style = Mock.Of<Style>();
            var setter = new Setter(TextBlock.TextProperty, CreateMockBinding(TextBlock.TextProperty));
            var activator = new Subject<bool>();

            setter.Apply(style, control.Object, activator);

            control.Verify(x => x.Bind(
                TextBlock.TextProperty,
                It.IsAny<IObservable<object>>(),
                BindingPriority.StyleTrigger));
        }

        private IBinding CreateMockBinding(AvaloniaProperty property)
        {
            var subject = new Subject<object>();
            var descriptor = InstancedBinding.OneWay(subject);
            var binding = Mock.Of<IBinding>(x => 
                x.Initiate(It.IsAny<IAvaloniaObject>(), property, null, false) == descriptor);
            return binding;
        }

        private class TestConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return value.ToString() + "bar";
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}
