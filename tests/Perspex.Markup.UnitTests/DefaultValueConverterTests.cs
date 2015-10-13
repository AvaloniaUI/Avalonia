// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Globalization;
using Xunit;

namespace Perspex.Markup.UnitTests
{
    public class DefaultValueConverterTests
    {
        [Fact]
        public void Can_Convert_String_To_Int()
        {
            var result = DefaultValueConverter.Instance.Convert(
                "5", 
                typeof(int), 
                null, 
                CultureInfo.InvariantCulture);

            Assert.Equal(5, result);
        }

        [Fact]
        public void Can_Convert_String_To_Double()
        {
            var result = DefaultValueConverter.Instance.Convert(
                "5",
                typeof(double),
                null,
                CultureInfo.InvariantCulture);

            Assert.Equal(5.0, result);
        }

        [Fact]
        public void Can_Convert_Double_To_String()
        {
            var result = DefaultValueConverter.Instance.Convert(
                5.0,
                typeof(string),
                null,
                CultureInfo.InvariantCulture);

            Assert.Equal("5", result);
        }
    }
}
