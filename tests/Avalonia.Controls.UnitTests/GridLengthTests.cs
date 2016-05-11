// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using System.Linq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class GridLengthTests
    {
        [Fact]
        public void Parse_Should_Parse_Auto()
        {
            var result = GridLength.Parse("Auto", CultureInfo.InvariantCulture);

            Assert.Equal(GridLength.Auto, result);
        }

        [Fact]
        public void Parse_Should_Parse_Auto_Lowercase()
        {
            var result = GridLength.Parse("auto", CultureInfo.InvariantCulture);

            Assert.Equal(GridLength.Auto, result);
        }

        [Fact]
        public void Parse_Should_Parse_Star()
        {
            var result = GridLength.Parse("*", CultureInfo.InvariantCulture);

            Assert.Equal(new GridLength(1, GridUnitType.Star), result);
        }

        [Fact]
        public void Parse_Should_Parse_Star_Value()
        {
            var result = GridLength.Parse("2*", CultureInfo.InvariantCulture);

            Assert.Equal(new GridLength(2, GridUnitType.Star), result);
        }

        [Fact]
        public void Parse_Should_Parse_Pixel_Value()
        {
            var result = GridLength.Parse("2", CultureInfo.InvariantCulture);

            Assert.Equal(new GridLength(2, GridUnitType.Pixel), result);
        }

        [Fact]
        public void Parse_Should_Throw_FormatException_For_Invalid_String()
        {
            Assert.Throws<FormatException>(() => GridLength.Parse("2x", CultureInfo.InvariantCulture));
        }

        [Fact]
        public void ParseLengths_Accepts_Comma_Separators()
        {
            var result = GridLength.ParseLengths("*,Auto,2*,4", CultureInfo.InvariantCulture).ToList();

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
            var result = GridLength.ParseLengths("* Auto 2* 4", CultureInfo.InvariantCulture).ToList();

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
        public void ParseLengths_Accepts_Comma_Separators_With_Spaces()
        {
            var result = GridLength.ParseLengths("*, Auto, 2* ,4", CultureInfo.InvariantCulture).ToList();

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
