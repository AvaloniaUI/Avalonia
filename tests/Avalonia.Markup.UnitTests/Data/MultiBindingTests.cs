using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using Moq;
using Avalonia.Controls;
using Xunit;
using System.Threading.Tasks;
using Avalonia.Data.Converters;
using Avalonia.Data;

namespace Avalonia.Markup.UnitTests.Data
{
    public class MultiBindingTests
    {
        [Fact]
        public async Task OneWay_Binding_Should_Be_Set_Up()
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
            var observable = binding.Initiate(target, null).Source;
            var result = await observable.Take(1);

            Assert.Equal("1,2,3", result);
        }

        [Fact]
        public async Task Nested_MultiBinding_Should_Be_Set_Up()
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
            var observable = binding.Initiate(target, null).Source;
            var result = await observable.Take(1);

            Assert.Equal("1,2,3", result);
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

        private class ConcatConverter : IMultiValueConverter
        {
            public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
            {
                return string.Join(",", values);
            }
        }

        private class UnsetValueConverter : IMultiValueConverter
        {
            public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
            {
                return AvaloniaProperty.UnsetValue;
            }
        }

        private class NullValueConverter : IMultiValueConverter
        {
            public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
            {
                return null;
            }
        }
    }
}
