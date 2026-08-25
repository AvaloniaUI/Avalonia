using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using Xunit;

namespace Avalonia.Base.UnitTests.Data
{
    public class ObjectConvertersTests
    {
        [Theory]
        [InlineData(null, null, null, true)]
        [InlineData(null, null, "value", false)]
        [InlineData(null, "value", null, false)]
        [InlineData("value", null, null, false)]
        [InlineData("value", "value", "value", false)]
        public void ObjectConvertersTests_AreAllNull_Works(object? value1, object? value2, object? value3, bool valid)
        {
            var converter = ObjectConverters.AreAllNull;
            var result = converter.Convert([value1, value2, value3], typeof(bool), null, CultureInfo.CurrentCulture);
            Assert.Equal(valid, Assert.IsType<bool>(result));
        }

        [Theory]
        [InlineData(null, null, null, true)]
        [InlineData(null, null, "value", true)]
        [InlineData(null, "value", null, true)]
        [InlineData("value", null, null, true)]
        [InlineData("value", "value", "value", false)]
        public void ObjectConvertersTests_AreAnyNull_Works(object? value1, object? value2, object? value3, bool valid)
        {
            var converter = ObjectConverters.AreAnyNull;
            var result = converter.Convert([value1, value2, value3], typeof(bool), null, CultureInfo.CurrentCulture);
            Assert.Equal(valid, Assert.IsType<bool>(result));
        }

        [Theory]
        [InlineData("value", "value", "value", true)]
        [InlineData(null, "value", null, false)]
        [InlineData("value", null, "value", false)]
        [InlineData("value", "value", "value1", false)]
        [InlineData("value1", "value", "value1", false)]
        [InlineData("value", "value", 1, false)]
        public void ObjectConvertersTests_AreEqual_Works(object? value1, object? value2, object? value3, bool valid)
        {
            var converter = ObjectConverters.AreAllEqual;
            var result = converter.Convert([value1, value2, value3], typeof(bool), null, CultureInfo.CurrentCulture);
            Assert.Equal(valid, Assert.IsType<bool>(result));
        }

        [Theory]
        [InlineData(true, true, true)]
        [InlineData(false, true, false)]
        [InlineData(false, false, true)]
        public void ObjectConvertersTests_AreEqual_Edge_Works(bool empty, bool unique, bool valid)
        {
            ICollection<object?> values;
            if (empty)
                values = Array.Empty<object?>();
            else if (unique)
                values = ["1", "2"];
            else
                values = ["1", "1"];

            var converter = ObjectConverters.AreAllEqual;
            var result = converter.Convert(values.ToList(), typeof(bool), null, CultureInfo.CurrentCulture);
            Assert.Equal(valid, Assert.IsType<bool>(result));
        }
    }
}
