// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Media
{
    public class ColorTests
    {
        [Fact]
        public void Parse_Parses_RGB_Hash_Color()
        {
            var result = Color.Parse("#ff8844");

            Assert.Equal(0xff, result.R);
            Assert.Equal(0x88, result.G);
            Assert.Equal(0x44, result.B);
            Assert.Equal(0xff, result.A);
        }

        [Fact]
        public void Parse_Parses_ARGB_Hash_Color()
        {
            var result = Color.Parse("#40ff8844");

            Assert.Equal(0xff, result.R);
            Assert.Equal(0x88, result.G);
            Assert.Equal(0x44, result.B);
            Assert.Equal(0x40, result.A);
        }

        [Fact]
        public void Parse_Parses_Named_Color_Lowercase()
        {
            var result = Color.Parse("red");

            Assert.Equal(0xff, result.R);
            Assert.Equal(0x00, result.G);
            Assert.Equal(0x00, result.B);
            Assert.Equal(0xff, result.A);
        }

        [Fact]
        public void Parse_Parses_Named_Color_Uppercase()
        {
            var result = Color.Parse("RED");

            Assert.Equal(0xff, result.R);
            Assert.Equal(0x00, result.G);
            Assert.Equal(0x00, result.B);
            Assert.Equal(0xff, result.A);
        }

        [Fact]
        public void Parse_Hex_Value_Doesnt_Accept_Too_Few_Chars()
        {
            Assert.Throws<FormatException>(() => Color.Parse("#ff"));
        }

        [Fact]
        public void Parse_Hex_Value_Doesnt_Accept_Too_Many_Chars()
        {
            Assert.Throws<FormatException>(() => Color.Parse("#ff5555555"));
        }

        [Fact]
        public void Parse_Hex_Value_Doesnt_Accept_Invalid_Number()
        {
            Assert.Throws<FormatException>(() => Color.Parse("#ff808g80"));
        }
    }
}
