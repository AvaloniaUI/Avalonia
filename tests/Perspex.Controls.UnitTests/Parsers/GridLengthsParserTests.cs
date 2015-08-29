// -----------------------------------------------------------------------
// <copyright file="GridLengthsParserTests.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.UnitTests.Parsers
{
    using System;
    using System.Linq;
    using Perspex.Controls.Parsers;
    using Xunit;

    public class GridLengthsParserTests
    {
        [Fact]
        public void Parser_Should_Correctly_Parse_Grid_Lengths()
        {
            var s = "*,Auto,2*,4px";
            var result = GridLengthsParser.Parse(s);

            Assert.Equal(
                new[]
                {
                    new GridLength(1, GridUnitType.Star),
                    GridLength.Auto,
                    new GridLength(2, GridUnitType.Star),
                    new GridLength(4, GridUnitType.Pixel),
                },
                result);
        }

        [Fact]
        public void Parser_Should_Throw_For_Invalid_Star_Value()
        {
            var s = "*,Auto,x*,4px";
            Assert.Throws<FormatException>(() => GridLengthsParser.Parse(s).ToList());
        }

        [Fact]
        public void Parser_Should_Throw_For_Invalid_Unit_Value()
        {
            var s = "*,Auto,4ab,4px";
            Assert.Throws<FormatException>(() => GridLengthsParser.Parse(s).ToList());
        }

        [Fact]
        public void Parser_Should_Throw_For_Empty_Entry()
        {
            var s = "*,Auto,,4px";
            Assert.Throws<FormatException>(() => GridLengthsParser.Parse(s).ToList());
        }
    }
}
