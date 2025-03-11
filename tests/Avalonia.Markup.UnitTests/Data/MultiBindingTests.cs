using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.UnitTests;
using Xunit;

#nullable enable

namespace Avalonia.Markup.UnitTests.Data
{
    public class MultiBindingTests
    {
        [Fact]
        public void OneWay_Binding_Should_Be_Set_Up()
        {
            var source = new { A = 1, B = 2, C = 3 };
            var binding = new MultiBinding
            {
                Converter = new ConcatConverter(),
                Bindings = new[]
                {
                    new Binding { Path = "A" },
                    new Binding { Path = "B" },
                    new Binding { Path = "C" },
                }
            };

            var target = new Control { DataContext = source };
            target.Bind(Control.TagProperty, binding);

            Assert.Equal("1,2,3", target.Tag);
        }

        [Fact]
        public void Nested_MultiBinding_Should_Be_Set_Up()
        {
            var source = new { A = 1, B = 2, C = 3 };
            var binding = new MultiBinding
            {
                Converter = new ConcatConverter(),
                Bindings =
                {
                    new Binding { Path = "A" },
                    new MultiBinding
                    {
                        Converter = new ConcatConverter(),
                        Bindings =
                        {
                            new Binding { Path = "B"},
                            new Binding { Path = "C"}
                        }
                    }
                }
            };

            var target = new Control { DataContext = source };
            target.Bind(Control.TagProperty, binding);

            Assert.Equal("1,2,3", target.Tag);
        }

        [Fact]
        public void Should_Return_FallbackValue_When_Converter_Returns_UnsetValue()
        {
            var target = new TextBlock();
            var source = new { A = 1, B = 2, C = 3 };
            var binding = new MultiBinding
            {
                Converter = new UnsetValueConverter(),
                Bindings = new[]
                {
                    new Binding { Path = "A" },
                    new Binding { Path = "B" },
                    new Binding { Path = "C" },
                },
                FallbackValue = "fallback",
            };

            target.Bind(TextBlock.TextProperty, binding);

            Assert.NotNull(target.Text);
            Assert.Equal("fallback", target.Text);
        }

        [Fact]
        public void Should_Return_TargetNullValue_When_Value_Is_Null()
        {
            var target = new TextBlock();

            var binding = new MultiBinding
            {
                Converter = new NullValueConverter(),
                Bindings = new[]
                {
                    new Binding { Path = "A" },
                    new Binding { Path = "B" },
                    new Binding { Path = "C" },
                },
                TargetNullValue = "(null)",
            };

            target.Bind(TextBlock.TextProperty, binding);

            Assert.NotNull(target.Text);
            Assert.Equal("(null)", target.Text);
        }

        [Fact]
        public void Should_Pass_UnsetValue_To_Converter_For_Broken_Binding()
        {
            var source = new { A = 1, B = 2, C = 3 };
            var target = new TextBlock { DataContext = source };

            var binding = new MultiBinding
            {
                Converter = new ConcatConverter(),
                Bindings = new[]
                {
                    new Binding { Path = "A" },
                    new Binding { Path = "B" },
                    new Binding { Path = "Missing" },
                },
            };

            target.Bind(TextBlock.TextProperty, binding);

            Assert.NotNull(target.Text);
            Assert.Equal("1,2,(unset)", target.Text);
        }

        [Fact]
        public void Should_Pass_FallbackValue_To_Converter_For_Broken_Binding()
        {
            var source = new { A = 1, B = 2, C = 3 };
            var target = new TextBlock { DataContext = source };

            var binding = new MultiBinding
            {
                Converter = new ConcatConverter(),
                Bindings = new[]
                {
                    new Binding { Path = "A" },
                    new Binding { Path = "B" },
                    new Binding { Path = "Missing", FallbackValue = "Fallback" },
                },
            };

            target.Bind(TextBlock.TextProperty, binding);

            Assert.NotNull(target.Text);
            Assert.Equal("1,2,Fallback", target.Text);
        }

        [Fact]
        public void MultiBinding_Without_StringFormat_And_Converter()
        {
            var source = new { A = 1, B = 2, C = 3 };
            var target = new ItemsControl { };

            var binding = new MultiBinding
            {
                Bindings = new[]
                {
                    new Binding { Path = "A", Source = source },
                    new Binding { Path = "B", Source = source },
                    new Binding { Path = "C", Source = source },
                },
            };

            target.Bind(ItemsControl.ItemsSourceProperty, binding);
            Assert.Equal(target.ItemCount, 3);
            Assert.Equal(target.ItemsView[0], source.A);
            Assert.Equal(target.ItemsView[1], source.B);
            Assert.Equal(target.ItemsView[2], source.C);
        }

        [Fact]
        public void Converter_Can_Return_BindingNotification()
        {
            var source = new { A = 1, B = 2, C = 3 };
            var target = new TextBlock { DataContext = source };

            var binding = new MultiBinding
            {
                Converter = new BindingNotificationConverter(),
                Bindings = new[]
                {
                    new Binding { Path = "A" },
                    new Binding { Path = "B" },
                    new Binding { Path = "C" },
                },
            };

            target.Bind(TextBlock.TextProperty, binding);

            Assert.Equal("1,2,3-BindingNotification", target.Text);
        }

        [Fact]
        public void Converter_Should_Be_Called_On_PropertyChanged_Even_If_Property_Not_Changed()
        {
            // Issue #16084
            var data = new TestModel();
            var target = new TextBlock { DataContext = data };

            var binding = new MultiBinding
            {
                Converter = new TestModelMemberConverter(),
                Bindings =
                {
                    new Binding(),
                    new Binding(nameof(data.NotifyingValue)),
                },
            };

            target.Bind(TextBlock.TextProperty, binding);
            Assert.Equal("0", target.Text);

            data.NonNotifyingValue = 1;
            Assert.Equal("0", target.Text);

            data.NotifyingValue = new object();
            Assert.Equal("1", target.Text);
        }

        private partial class TestModel : NotifyingBase
        {
            private object? _notifyingValue;

            public int? NonNotifyingValue { get; set; } = 0;

            public object? NotifyingValue
            {
                get => _notifyingValue;
                set => SetField(ref _notifyingValue, value);
            }
        }

        private class ConcatConverter : IMultiValueConverter
        {
            public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
            {
                return string.Join(",", values);
            }
        }

        private class UnsetValueConverter : IMultiValueConverter
        {
            public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
            {
                return AvaloniaProperty.UnsetValue;
            }
        }

        private class NullValueConverter : IMultiValueConverter
        {
            public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
            {
                return null;
            }
        }

        private class BindingNotificationConverter : IMultiValueConverter
        {
            public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
            {
                return new BindingNotification(
                    new ArgumentException(),
                    BindingErrorType.Error,
                    string.Join(",", values) + "-BindingNotification");
            }
        }

        private class TestModelMemberConverter : IMultiValueConverter
        {
            public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
            {
                if (values[0] is not TestModel model)
                {
                    return string.Empty;
                }

                return model.NonNotifyingValue.ToString();
            }
        }
    }
}
