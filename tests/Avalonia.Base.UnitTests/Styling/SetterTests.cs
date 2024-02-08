using System;
using System.Globalization;
using System.Reactive.Subjects;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Moq;
using Xunit;

#nullable enable

namespace Avalonia.Base.UnitTests.Styling
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
            var binding = subject.ToBinding();
            var setter = new Setter(TextBlock.TagProperty, binding);

            Apply(setter, control);

            Assert.Equal("foo", control.Tag);
        }

        [Fact]
        public void Setter_Should_Handle_Binding_Producing_UnsetValue()
        {
            var control = new TextBlock();
            var subject = new BehaviorSubject<object>(AvaloniaProperty.UnsetValue);
            var binding = subject.ToBinding();
            var setter = new Setter(TextBlock.TagProperty, binding);

            Apply(setter, control);

            Assert.Equal(null, control.Text);
        }

        [Fact]
        public void Setter_Should_Materialize_Template_To_Property()
        {
            var control = new Decorator();
            var template = new FuncTemplate<Canvas>(() => new Canvas());
            var style = Mock.Of<IStyle>();
            var setter = new Setter(Decorator.ChildProperty, template);

            Apply(setter, control);

            Assert.IsType<Canvas>(control.Child);
        }

        [Fact]
        public void Can_Set_Direct_Property_In_Style_Without_Activator()
        {
            var control = new DirectPropertyClass();
            var target = new Setter();
            var style = new Style(x => x.Is<DirectPropertyClass>())
            {
                Setters =
                {
                    new Setter(DirectPropertyClass.FooProperty, "foo"),
                }
            };

            Apply(style, control);

            Assert.Equal("foo", control.Foo);
        }

        [Fact]
        public void Can_Set_Direct_Property_Binding_In_Style_Without_Activator()
        {
            var control = new DirectPropertyClass();
            var target = new Setter();
            var source = new BehaviorSubject<object?>("foo");
            var style = new Style(x => x.Is<DirectPropertyClass>())
            {
                Setters =
                {
                    new Setter(DirectPropertyClass.FooProperty, source.ToBinding()),
                }
            };

            Apply(style, control);

            Assert.Equal("foo", control.Foo);
        }

        [Fact]
        public void Cannot_Set_Direct_Property_Binding_In_Style_With_Activator()
        {
            var control = new DirectPropertyClass();
            var target = new Setter();
            var source = new BehaviorSubject<object?>("foo");
            var style = new Style(x => x.Is<DirectPropertyClass>().Class("foo"))
            {
                Setters =
                {
                    new Setter(DirectPropertyClass.FooProperty, source.ToBinding()),
                }
            };

            Assert.Throws<InvalidOperationException>(() => Apply(style, control));
        }

        [Fact]
        public void Cannot_Set_Direct_Property_In_Style_With_Activator()
        {
            var control = new DirectPropertyClass();
            var target = new Setter();
            var style = new Style(x => x.Is<DirectPropertyClass>().Class("foo"))
            {
                Setters =
                {
                    new Setter(DirectPropertyClass.FooProperty, "foo"),
                }
            };

            Assert.Throws<InvalidOperationException>(() => Apply(style, control));
        }

        [Fact]
        public void Does_Not_Call_Converter_ConvertBack_On_OneWay_Binding()
        {
            var control = new Decorator
            {
                Name = "foo",
                Classes = { "foo" },
            };

            var binding = new Binding("Name", BindingMode.OneWay)
            {
                Converter = new TestConverter(),
                RelativeSource = new RelativeSource(RelativeSourceMode.Self),
            };

            var style = new Style(x => x.OfType<Decorator>().Class("foo"))
            {
                Setters =
                {
                    new Setter(Decorator.TagProperty, binding)
                },
            };

            Apply(style, control);

            Assert.Equal("foobar", control.Tag);

            // Issue #1218 caused TestConverter.ConvertBack to throw here.
            control.Classes.Remove("foo");
            Assert.Null(control.Tag);
        }

        [Fact]
        public void Setter_Should_Apply_Value_Without_Activator_With_Style_Priority()
        {
            var control = new Border();
            var style = new Style(x => x.OfType<Border>())
            {
                Setters =
                {
                    new Setter(Control.TagProperty, "foo"),
                },
            };
            var raised = 0;

            control.PropertyChanged += (s, e) =>
            {
                Assert.Equal(Control.TagProperty, e.Property);
                Assert.Equal(BindingPriority.Style, e.Priority);
                ++raised;
            };

            Apply(style, control);

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Setter_Should_Apply_Value_With_Activator_With_StyleTrigger_Priority()
        {
            var control = new Border { Classes = { "foo" } };
            var style = new Style(x => x.OfType<Border>().Class("foo"))
            {
                Setters =
                {
                    new Setter(Control.TagProperty, "foo"),
                },
            };
            var activator = new Subject<bool>();
            var raised = 0;

            control.PropertyChanged += (s, e) =>
            {
                Assert.Equal(Border.TagProperty, e.Property);
                Assert.Equal(BindingPriority.StyleTrigger, e.Priority);
                ++raised;
            };

            Apply(style, control);

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Setter_Should_Apply_Binding_Without_Activator_With_Style_Priority()
        {
            var control = new Border
            {
                DataContext = "foo",
            };

            var style = new Style(x => x.OfType<Border>())
            {
                Setters =
                {
                    new Setter(Control.TagProperty, new Binding()),
                },
            };

            var raised = 0;

            control.PropertyChanged += (s, e) =>
            {
                Assert.Equal(Control.TagProperty, e.Property);
                Assert.Equal(BindingPriority.Style, e.Priority);
                ++raised;
            };

            Apply(style, control);

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Setter_Should_Apply_Binding_With_Activator_With_StyleTrigger_Priority()
        {
            var control = new Border
            {
                Classes = { "foo" },
                DataContext = "foo",
            };

            var style = new Style(x => x.OfType<Border>().Class("foo"))
            {
                Setters =
                {
                    new Setter(Control.TagProperty, new Binding()),
                },
            };

            var raised = 0;

            control.PropertyChanged += (s, e) =>
            {
                Assert.Equal(Control.TagProperty, e.Property);
                Assert.Equal(BindingPriority.StyleTrigger, e.Priority);
                ++raised;
            };

            Apply(style, control);

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Direct_Property_Setter_With_TwoWay_Binding_Should_Update_Source()
        {
            using var app = UnitTestApplication.Start(TestServices.MockThreadingInterface);
            var data = new Data { Foo = "foo" };
            var control = new DirectPropertyClass
            {
                DataContext = data,
            };

            var style = new Style(x => x.OfType<DirectPropertyClass>())
            {
                Setters =
                {
                    new Setter
                    {
                        Property = DirectPropertyClass.FooProperty,
                        Value = new Binding
                        {
                            Path = "Foo",
                            Mode = BindingMode.TwoWay
                        }
                    }
                },
            };

            Apply(style, control);
            Assert.Equal("foo", control.Foo);

            control.Foo = "bar";
            Assert.Equal("bar", data.Foo);
        }

        [Fact]
        public void Styled_Property_Setter_With_TwoWay_Binding_Should_Update_Source()
        {
            var data = new Data { Bar = Brushes.Red };
            var control = new Border
            {
                DataContext = data,
            };

            var style = new Style(x => x.OfType<Border>())
            {
                Setters =
                {
                    new Setter
                    {
                        Property = Border.BackgroundProperty,
                        Value = new Binding
                        {
                            Path = "Bar",
                            Mode = BindingMode.TwoWay
                        }
                    }
                },
            };

            Apply(style, control);
            Assert.Equal(Brushes.Red, control.Background);

            control.Background = Brushes.Green;
            Assert.Equal(Brushes.Green, data.Bar);
        }

        [Fact]
        public void Non_Active_Styled_Property_Binding_Should_Be_Unsubscribed()
        {
            var data = new Data { Bar = Brushes.Red };
            var control = new Border
            {
                DataContext = data,
            };

            var style1 = new Style(x => x.OfType<Border>())
            {
                Setters =
                {
                    new Setter
                    {
                        Property = Border.BackgroundProperty,
                        Value = new Binding("Bar"),
                    }
                },
            };

            var style2 = new Style(x => x.OfType<Border>().Class("foo"))
            {
                Setters =
                {
                    new Setter
                    {
                        Property = Border.BackgroundProperty,
                        Value = Brushes.Green,
                    }
                },
            };

            Apply(style1, control);
            Apply(style2, control);

            // `style1` is initially active.
            Assert.Equal(Brushes.Red, control.Background);
            Assert.Equal(1, data.PropertyChangedSubscriptionCount);

            // Activate `style2`.
            control.Classes.Add("foo");
            Assert.Equal(Brushes.Green, control.Background);

            // The binding from `style1` is now inactive and so should be unsubscribed.
            Assert.Equal(0, data.PropertyChangedSubscriptionCount);
        }

        [Fact]
        public void Non_Active_Styled_Property_Setter_With_TwoWay_Binding_Should_Not_Update_Source()
        {
            var data = new Data { Bar = Brushes.Red };
            var control = new Border
            {
                DataContext = data,
            };

            var style1 = new Style(x => x.OfType<Border>())
            {
                Setters =
                {
                    new Setter
                    {
                        Property = Border.BackgroundProperty,
                        Value = new Binding
                        {
                            Path = "Bar",
                            Mode = BindingMode.TwoWay
                        }
                    }
                },
            };

            var style2 = new Style(x => x.OfType<Border>().Class("foo"))
            {
                Setters =
                {
                    new Setter
                    {
                        Property = Border.BackgroundProperty,
                        Value = Brushes.Green,
                    }
                },
            };

            Apply(style1, control);
            Apply(style2, control);

            // `style1` is initially active.
            Assert.Equal(Brushes.Red, control.Background);

            // Activate `style2`.
            control.Classes.Add("foo");
            Assert.Equal(Brushes.Green, control.Background);

            // The two-way binding from `style1` is now inactive and so should not write back to
            // the DataContext.
            Assert.Equal(Brushes.Red, data.Bar);
        }

        [Fact]
        public void Styled_Property_Setter_With_TwoWay_Binding_Updates_Source_When_Made_Active()
        {
            var data = new Data { Bar = Brushes.Red };
            var control = new Border
            {
                Classes = { "foo" },
                DataContext = data,
            };

            var style1 = new Style(x => x.OfType<Border>())
            {
                Setters =
                {
                    new Setter
                    {
                        Property = Border.BackgroundProperty,
                        Value = new Binding
                        {
                            Path = "Bar",
                            Mode = BindingMode.TwoWay
                        }
                    }
                },
            };

            var style2 = new Style(x => x.OfType<Border>().Class("foo"))
            {
                Setters =
                {
                    new Setter
                    {
                        Property = Border.BackgroundProperty,
                        Value = Brushes.Green,
                    }
                },
            };

            Apply(style1, control);
            Apply(style2, control);

            // `style2` is initially active.
            Assert.Equal(Brushes.Green, control.Background);

            // Deactivate `style2`.
            control.Classes.Remove("foo");
            Assert.Equal(Brushes.Red, control.Background);

            // The two-way binding from `style1` is now active and so should write back to the
            // DataContext.
            control.Background = Brushes.Blue;
            Assert.Equal(Brushes.Blue, data.Bar);
        }

        private void Apply(Style style, StyledElement element)
        {
            StyleHelpers.TryAttach(style, element);
        }

        private void Apply(Setter setter, Control control)
        {
            var style = new Style(x => x.Is<Control>())
            {
                Setters = { setter },
            };

            Apply(style, control);
        }

        private class Data : NotifyingBase
        {
            public string? Foo { get; set; }
            public IBrush? Bar { get; set; }
        }

        private class TestConverter : IValueConverter
        {
            public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                return value + "bar";
            }

            public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        private class DirectPropertyClass : StyledElement
        {
            public static readonly DirectProperty<DirectPropertyClass, string?> FooProperty = AvaloniaProperty.RegisterDirect<DirectPropertyClass, string?>(nameof(Foo),
                x => x.Foo, (x, v) => x.Foo = v);
            
            private string? _foo;
            public string? Foo
            {
                get => _foo;
                set => SetAndRaise(FooProperty, ref _foo, value);
            }
        }
    }
}
