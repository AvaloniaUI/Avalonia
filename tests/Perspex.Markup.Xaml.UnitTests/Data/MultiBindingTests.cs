// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Moq;
using Perspex.Controls;
using Perspex.Markup.Xaml.Data;
using Xunit;

namespace Perspex.Markup.Xaml.UnitTests.Data
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
                    new Binding { SourcePropertyPath = "A" },
                    new Binding { SourcePropertyPath = "B" },
                    new Binding { SourcePropertyPath = "C" },
                }
            };

            var target = new Mock<IObservablePropertyBag>();
            target.Setup(x => x.GetValue(Control.DataContextProperty)).Returns(source);
            target.Setup(x => x.GetObservable(Control.DataContextProperty)).Returns(
                Observable.Never<object>().StartWith(source));

            var subject = binding.CreateSubject(target.Object, typeof(string));
            var result = await subject.Take(1);

            Assert.Equal("1,2,3", result);
        }

        private class ConcatConverter : IMultiValueConverter
        {
            public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
            {
                return string.Join(",", values);
            }
        }
    }
}
