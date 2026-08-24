using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class FlexBasisTests : ScopedTestBase
    {
        [Fact]
        public void Parse_Should_Parse_Auto()
        {
            var result = FlexBasis.Parse("Auto");

            Assert.Equal(FlexBasis.Auto, result);
        }

        [Fact]
        public void Parse_Should_Parse_Auto_Lowercase()
        {
            var result = FlexBasis.Parse("auto");

            Assert.Equal(FlexBasis.Auto, result);
        }

        [Fact]
        public void Parse_Should_Parse_Percentage()
        {
            var result = FlexBasis.Parse("50%");

            Assert.Equal(new FlexBasis(0.5, FlexBasisKind.Relative), result);
        }

        [Fact]
        public void Parse_Should_Parse_Absolute_Value()
        {
            var result = FlexBasis.Parse("2");

            Assert.Equal(new FlexBasis(2, FlexBasisKind.Absolute), result);
        }

        [Fact]
        public void Parse_Should_Throw_ArgumentException_For_Invalid_String()
        {
            Assert.Throws<ArgumentException>(() => FlexBasis.Parse("2x"));
        }
    }
}
