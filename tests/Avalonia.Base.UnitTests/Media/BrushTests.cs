using System;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class BrushTests
    {
        [Fact]
        public void Parse_Parses_RGB_Hash_Brush()
        {
            var result = (ISolidColorBrush)Brush.Parse("#ff8844");

            Assert.Equal(0xff, result.Color.R);
            Assert.Equal(0x88, result.Color.G);
            Assert.Equal(0x44, result.Color.B);
            Assert.Equal(0xff, result.Color.A);
        }

        [Fact]
        public void Parse_Parses_ARGB_Hash_Brush()
        {
            var result = (ISolidColorBrush)Brush.Parse("#40ff8844");

            Assert.Equal(0xff, result.Color.R);
            Assert.Equal(0x88, result.Color.G);
            Assert.Equal(0x44, result.Color.B);
            Assert.Equal(0x40, result.Color.A);
        }

        [Fact]
        public void Parse_Parses_Named_Brush_Lowercase()
        {
            var result = (ISolidColorBrush)Brush.Parse("red");

            Assert.Equal(0xff, result.Color.R);
            Assert.Equal(0x00, result.Color.G);
            Assert.Equal(0x00, result.Color.B);
            Assert.Equal(0xff, result.Color.A);
        }

        [Fact]
        public void Parse_Parses_Named_Brush_Uppercase()
        {
            var result = (ISolidColorBrush)Brush.Parse("RED");

            Assert.Equal(0xff, result.Color.R);
            Assert.Equal(0x00, result.Color.G);
            Assert.Equal(0x00, result.Color.B);
            Assert.Equal(0xff, result.Color.A);
        }

        [Fact]
        public void Parse_ToString_Named_Brush_Roundtrip()
        {
            const string expectedName = "Red";
            var brush = (ISolidColorBrush)Brush.Parse(expectedName);
            var name = brush.ToString();

            Assert.Equal(expectedName, name);
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

        [Theory]
        [InlineData("rgb(255, 128, 64)")]
        [InlineData("rgba(255, 128, 64, 0.5)")]
        [InlineData("hsl(120, 100%, 50%)")]
        [InlineData("hsla(120, 100%, 50%, 0.5)")]
        [InlineData("hsv(300, 100%, 25%)")]
        [InlineData("hsva(300, 100%, 25%, 0.75)")]
        [InlineData("#40ff8844")]
        [InlineData("Green")]
        public void Parse_Parses_All_Color_Format_Brushes(string input)
        {
            var brush = Brush.Parse(input);
            Assert.IsAssignableFrom<ISolidColorBrush>(brush);

            // The ColorTests already validate all color formats are parsed properly
            // Since Brush.Parse() forwards to Color.Parse() we don't need to repeat this
            // We can simply check if the parsed Brush's color matches what Color.Parse provides
            var expected = Color.Parse(input);
            Assert.Equal(expected, (brush as ISolidColorBrush)?.Color);
        }

        [Fact]
        public void Changing_Opacity_Raises_Invalidated()
        {
            var target = new SolidColorBrush();

            RenderResourceTestHelper.AssertResourceInvalidation(target, () => { target.Opacity = 0.5; });
        }
    }
}
