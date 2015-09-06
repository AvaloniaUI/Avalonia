// -----------------------------------------------------------------------
// <copyright file="GridLengthTests.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.UnitTests
{
    using System;
    using System.Linq;
    using Xunit;

    public class GridLengthTests
    {
        [Fact]
        public void Parse_Should_Parse_Auto()
        {
            var result = GridLength.Parse("Auto");

            Assert.Equal(GridLength.Auto, result);
        }

        [Fact]
        public void Parse_Should_Parse_Auto_Lowercase()
        {
            var result = GridLength.Parse("auto");

            Assert.Equal(GridLength.Auto, result);
        }

        [Fact]
        public void Parse_Should_Parse_Star()
        {
            var result = GridLength.Parse("*");

            Assert.Equal(new GridLength(1, GridUnitType.Star), result);
        }

        [Fact]
        public void Parse_Should_Parse_Star_Value()
        {
            var result = GridLength.Parse("2*");

            Assert.Equal(new GridLength(2, GridUnitType.Star), result);
        }

        [Fact]
        public void Parse_Should_Parse_Pixel_Value()
        {
            var result = GridLength.Parse("2");

            Assert.Equal(new GridLength(2, GridUnitType.Pixel), result);
        }

        [Fact]
        public void Parse_Should_Throw_FormatException_For_Invalid_String()
        {
            Assert.Throws<FormatException>(() => GridLength.Parse("2x"));
        }

        [Fact]
        public void ParseLengths_Accepts_Comma_Separators()
        {
            var result = GridLength.ParseLengths("*,Auto,2*,4").ToList();

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
        public void ParseLengths_Accepts_Space_Separators()
        {
            var result = GridLength.ParseLengths("* Auto 2* 4").ToList();

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
    }
}
