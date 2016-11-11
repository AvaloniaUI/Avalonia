// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Media
{
    public class BrushTests
    {
        [Fact]
        public void Parse_Parses_RGB_Hash_Brush()
        {
            var result = (SolidColorBrush)Brush.Parse("#ff8844");

            Assert.Equal(0xff, result.Color.R);
            Assert.Equal(0x88, result.Color.G);
            Assert.Equal(0x44, result.Color.B);
            Assert.Equal(0xff, result.Color.A);
        }

        [Fact]
        public void Parse_Parses_ARGB_Hash_Brush()
        {
            var result = (SolidColorBrush)Brush.Parse("#40ff8844");

            Assert.Equal(0xff, result.Color.R);
            Assert.Equal(0x88, result.Color.G);
            Assert.Equal(0x44, result.Color.B);
            Assert.Equal(0x40, result.Color.A);
        }

        [Fact]
        public void Parse_Parses_Named_Brush_Lowercase()
        {
            var result = (SolidColorBrush)Brush.Parse("red");

            Assert.Equal(0xff, result.Color.R);
            Assert.Equal(0x00, result.Color.G);
            Assert.Equal(0x00, result.Color.B);
            Assert.Equal(0xff, result.Color.A);
        }

        [Fact]
        public void Parse_Parses_Named_Brush_Uppercase()
        {
            var result = (SolidColorBrush)Brush.Parse("RED");

            Assert.Equal(0xff, result.Color.R);
            Assert.Equal(0x00, result.Color.G);
            Assert.Equal(0x00, result.Color.B);
            Assert.Equal(0xff, result.Color.A);
        }

        [Fact]
        public void Parse_Hex_Value_Doesnt_Accept_Too_Few_Chars()
        {
            Assert.Throws<FormatException>(() => Brush.Parse("#ff"));
        }

        [Fact]
        public void Parse_Hex_Value_Doesnt_Accept_Too_Many_Chars()
        {
            Assert.Throws<FormatException>(() => Brush.Parse("#ff5555555"));
        }

        [Fact]
        public void Parse_Hex_Value_Doesnt_Accept_Invalid_Number()
        {
            Assert.Throws<FormatException>(() => Brush.Parse("#ff808g80"));
        }
    }
}
