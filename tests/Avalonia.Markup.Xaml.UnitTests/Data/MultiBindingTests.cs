// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using Moq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Data;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Data
{
    public class MultiBindingTests
    {
        [Fact]
        public async void OneWay_Binding_Should_Be_Set_Up()
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

            var target = new Mock<IAvaloniaObject>().As<IControl>();
            target.Setup(x => x.GetValue(Control.DataContextProperty)).Returns(source);

            var subject = binding.Initiate(target.Object, null).Subject;
            var result = await subject.Take(1);

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

            Assert.Equal("fallback", target.Text);
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
    }
}
