using System;
using System.Globalization;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
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
        public void Try_Parse_Parses_RGB_Hash_Color()
        {
            var success = Color.TryParse("#ff8844", out Color result);

            Assert.True(success);
            Assert.Equal(0xff, result.R);
            Assert.Equal(0x88, result.G);
            Assert.Equal(0x44, result.B);
            Assert.Equal(0xff, result.A);
        }

        [Fact]
        public void Parse_Parses_RGB_Hash_Shorthand_Color()
        {
            var result = Color.Parse("#f84");

            Assert.Equal(0xff, result.R);
            Assert.Equal(0x88, result.G);
            Assert.Equal(0x44, result.B);
            Assert.Equal(0xff, result.A);
        }

        [Fact]
        public void Try_Parse_Parses_RGB_Hash_Shorthand_Color()
        {
            var success = Color.TryParse("#f84", out Color result);

            Assert.True(success);
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
        public void Try_Parse_Parses_ARGB_Hash_Color()
        {
            var success = Color.TryParse("#40ff8844", out Color result);

            Assert.True(success);
            Assert.Equal(0xff, result.R);
            Assert.Equal(0x88, result.G);
            Assert.Equal(0x44, result.B);
            Assert.Equal(0x40, result.A);
        }

        [Fact]
        public void Parse_Parses_ARGB_Hash_Shorthand_Color()
        {
            var result = Color.Parse("#4f84");

            Assert.Equal(0xff, result.R);
            Assert.Equal(0x88, result.G);
            Assert.Equal(0x44, result.B);
            Assert.Equal(0x44, result.A);
        }

        [Fact]
        public void Try_Parse_Parses_ARGB_Hash_Shorthand_Color()
        {
            var success = Color.TryParse("#4f84", out Color result);

            Assert.True(success);
            Assert.Equal(0xff, result.R);
            Assert.Equal(0x88, result.G);
            Assert.Equal(0x44, result.B);
            Assert.Equal(0x44, result.A);
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
        public void TryParse_Parses_Named_Color_Lowercase()
        {
            var success = Color.TryParse("red", out Color result);

            Assert.True(success);
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
        public void TryParse_Parses_Named_Color_Uppercase()
        {
            var success = Color.TryParse("RED", out Color result);

            Assert.True(success);
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
        public void TryParse_Hex_Value_Doesnt_Accept_Too_Few_Chars()
        {
            Assert.False(Color.TryParse("#ff", out _));
        }

        [Fact]
        public void Parse_Hex_Value_Doesnt_Accept_Too_Many_Chars()
        {
            Assert.Throws<FormatException>(() => Color.Parse("#ff5555555"));
        }

        [Fact]
        public void TryParse_Hex_Value_Doesnt_Accept_Too_Many_Chars()
        {
            Assert.False(Color.TryParse("#ff5555555", out _));
        }

        [Fact]
        public void Parse_Hex_Value_Doesnt_Accept_Invalid_Number()
        {
            Assert.Throws<FormatException>(() => Color.Parse("#ff808g80"));
        }

        [Fact]
        public void TryParse_Hex_Value_Doesnt_Accept_Invalid_Number()
        {
            Assert.False(Color.TryParse("#ff808g80", out _));
        }

        [Fact]
        public void Parse_Throws_ArgumentNullException_For_Null_Input()
        {
            Assert.Throws<ArgumentNullException>(() => Color.Parse(null!));
        }

        [Fact]
        public void Parse_Throws_FormatException_For_Invalid_Input()
        {
            Assert.Throws<FormatException>(() => Color.Parse(string.Empty));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void TryParse_Returns_False_For_Invalid_Input(string? input)
        {
            Assert.False(Color.TryParse(input, out _));
        }

        [Fact]
        public void Try_Parse_HslColor()
        {
            // Inline data requires constants, so the data is handled internally here
            var data = new Tuple<string, HslColor>[]
            {
                // HSV
                Tuple.Create("hsl(0, 0, 0)",         new HslColor(1, 0, 0, 0)),
                Tuple.Create("hsl(0, 0%, 0%)",       new HslColor(1, 0, 0, 0)),
                Tuple.Create("hsl(180, 0.5, 0.5)",   new HslColor(1, 180, 0.5, 0.5)),
                Tuple.Create("hsl(180, 50%, 50%)",   new HslColor(1, 180, 0.5, 0.5)),
                Tuple.Create("hsl(360, 1.0, 1.0)",   new HslColor(1, 0, 1, 1)),         // Wraps Hue to zero
                Tuple.Create("hsl(360, 100%, 100%)", new HslColor(1, 0, 1, 1)),         // Wraps Hue to zero

                Tuple.Create("hsl(-1000, -1000, -1000)",   new HslColor(1, 0, 0, 0)),   // Clamps to min
                Tuple.Create("hsl(-1000, -1000%, -1000%)", new HslColor(1, 0, 0, 0)),   // Clamps to min
                Tuple.Create("hsl(1000, 1000, 1000)",      new HslColor(1, 0, 1, 1)),   // Clamps to max (Hue wraps to zero)
                Tuple.Create("hsl(1000, 1000%, 1000%)",    new HslColor(1, 0, 1, 1)),   // Clamps to max (Hue wraps to zero)

                Tuple.Create("hsl(300, 0.8, 0.2)", new HslColor(1.0, 300, 0.8, 0.2)),
                Tuple.Create("hsl(300, 80%, 20%)", new HslColor(1.0, 300, 0.8, 0.2)),

                // HSVA
                Tuple.Create("hsla(0, 0, 0, 0)",            new HslColor(0, 0, 0, 0)),
                Tuple.Create("hsla(0, 0%, 0%, 0%)",         new HslColor(0, 0, 0, 0)),
                Tuple.Create("hsla(180, 0.5, 0.5, 0.5)",    new HslColor(0.5, 180, 0.5, 0.5)),
                Tuple.Create("hsla(180, 50%, 50%, 50%)",    new HslColor(0.5, 180, 0.5, 0.5)),
                Tuple.Create("hsla(360, 1.0, 1.0, 1.0)",    new HslColor(1, 0, 1, 1)),          // Wraps Hue to zero
                Tuple.Create("hsla(360, 100%, 100%, 100%)", new HslColor(1, 0, 1, 1)),          // Wraps Hue to zero

                Tuple.Create("hsla(-1000, -1000, -1000, -1000)",    new HslColor(0, 0, 0, 0)),  // Clamps to min
                Tuple.Create("hsla(-1000, -1000%, -1000%, -1000%)", new HslColor(0, 0, 0, 0)),  // Clamps to min
                Tuple.Create("hsla(1000, 1000, 1000, 1000)",        new HslColor(1, 0, 1, 1)),  // Clamps to max (Hue wraps to zero)
                Tuple.Create("hsla(1000, 1000%, 1000%, 1000%)",     new HslColor(1, 0, 1, 1)),  // Clamps to max (Hue wraps to zero)

                Tuple.Create("hsla(300, 0.9, 0.2, 0.8)", new HslColor(0.8, 300, 0.9, 0.2)),
                Tuple.Create("hsla(300, 90%, 20%, 0.8)", new HslColor(0.8, 300, 0.9, 0.2)),
            };

            foreach (var dataPoint in data)
            {
                Assert.True(HslColor.TryParse(dataPoint.Item1, out HslColor parsedHslColor));
                Assert.True(dataPoint.Item2 == parsedHslColor);
            }
        }

        [Fact]
        public void Try_Parse_HsvColor()
        {
            // Inline data requires constants, so the data is handled internally here
            var data = new Tuple<string, HsvColor>[]
            {
                // HSV
                Tuple.Create("hsv(0, 0, 0)",         new HsvColor(1, 0, 0, 0)),
                Tuple.Create("hsv(0, 0%, 0%)",       new HsvColor(1, 0, 0, 0)),
                Tuple.Create("hsv(180, 0.5, 0.5)",   new HsvColor(1, 180, 0.5, 0.5)),
                Tuple.Create("hsv(180, 50%, 50%)",   new HsvColor(1, 180, 0.5, 0.5)),
                Tuple.Create("hsv(360, 1.0, 1.0)",   new HsvColor(1, 0, 1, 1)),         // Wraps Hue to zero
                Tuple.Create("hsv(360, 100%, 100%)", new HsvColor(1, 0, 1, 1)),         // Wraps Hue to zero

                Tuple.Create("hsv(-1000, -1000, -1000)",   new HsvColor(1, 0, 0, 0)),   // Clamps to min
                Tuple.Create("hsv(-1000, -1000%, -1000%)", new HsvColor(1, 0, 0, 0)),   // Clamps to min
                Tuple.Create("hsv(1000, 1000, 1000)",      new HsvColor(1, 0, 1, 1)),   // Clamps to max (Hue wraps to zero)
                Tuple.Create("hsv(1000, 1000%, 1000%)",    new HsvColor(1, 0, 1, 1)),   // Clamps to max (Hue wraps to zero)

                Tuple.Create("hsv(300, 0.8, 0.2)", new HsvColor(1.0, 300, 0.8, 0.2)),
                Tuple.Create("hsv(300, 80%, 20%)", new HsvColor(1.0, 300, 0.8, 0.2)),

                // HSVA
                Tuple.Create("hsva(0, 0, 0, 0)",            new HsvColor(0, 0, 0, 0)),
                Tuple.Create("hsva(0, 0%, 0%, 0%)",         new HsvColor(0, 0, 0, 0)),
                Tuple.Create("hsva(180, 0.5, 0.5, 0.5)",    new HsvColor(0.5, 180, 0.5, 0.5)),
                Tuple.Create("hsva(180, 50%, 50%, 50%)",    new HsvColor(0.5, 180, 0.5, 0.5)),
                Tuple.Create("hsva(360, 1.0, 1.0, 1.0)",    new HsvColor(1, 0, 1, 1)),          // Wraps Hue to zero
                Tuple.Create("hsva(360, 100%, 100%, 100%)", new HsvColor(1, 0, 1, 1)),          // Wraps Hue to zero

                Tuple.Create("hsva(-1000, -1000, -1000, -1000)",    new HsvColor(0, 0, 0, 0)),  // Clamps to min
                Tuple.Create("hsva(-1000, -1000%, -1000%, -1000%)", new HsvColor(0, 0, 0, 0)),  // Clamps to min
                Tuple.Create("hsva(1000, 1000, 1000, 1000)",        new HsvColor(1, 0, 1, 1)),  // Clamps to max (Hue wraps to zero)
                Tuple.Create("hsva(1000, 1000%, 1000%, 1000%)",     new HsvColor(1, 0, 1, 1)),  // Clamps to max (Hue wraps to zero)

                Tuple.Create("hsva(300, 0.9, 0.2, 0.8)", new HsvColor(0.8, 300, 0.9, 0.2)),
                Tuple.Create("hsva(300, 90%, 20%, 0.8)", new HsvColor(0.8, 300, 0.9, 0.2)),
            };

            foreach (var dataPoint in data)
            {
                Assert.True(HsvColor.TryParse(dataPoint.Item1, out HsvColor parsedHsvColor));
                Assert.True(dataPoint.Item2 == parsedHsvColor);
            }
        }

        [Fact]
        public void Try_Parse_All_Formats_With_Conversion()
        {
            // Inline data requires constants, so the data is handled internally here
            var data = new Tuple<string, Color>[]
            {
                // RGB
                Tuple.Create("White",   new Color(0xff, 0xff, 0xff, 0xff)),
                Tuple.Create("#123456", new Color(0xff, 0x12, 0x34, 0x56)),

                Tuple.Create("rgb(100, 30, 45)",       new Color(255, 100, 30, 45)),
                Tuple.Create("rgba(100, 30, 45, 0.9)", new Color(230, 100, 30, 45)),
                Tuple.Create("rgba(100, 30, 45, 90%)", new Color(230, 100, 30, 45)),

                Tuple.Create("rgb(255,0,0)", new Color(255, 255, 0, 0)),
                Tuple.Create("rgb(0,255,0)", new Color(255, 0, 255, 0)),
                Tuple.Create("rgb(0,0,255)", new Color(255, 0, 0, 255)),

                Tuple.Create("rgb(100%, 0, 0)", new Color(255, 255, 0, 0)),
                Tuple.Create("rgb(0, 100%, 0)", new Color(255, 0, 255, 0)),
                Tuple.Create("rgb(0, 0, 100%)", new Color(255, 0, 0, 255)),

                Tuple.Create("rgba(0, 0, 100%, 50%)",    new Color(128, 0, 0, 255)),
                Tuple.Create("rgba(50%, 10%, 80%, 50%)", new Color(128, 128, 26, 204)),
                Tuple.Create("rgba(50%, 10%, 80%, 0.5)", new Color(128, 128, 26, 204)),

                // HSL
                Tuple.Create("hsl(296, 85%, 12%)",         new Color(255, 53, 5, 57)),
                Tuple.Create("hsla(296, 0.85, 0.12, 0.9)", new Color(230, 53, 5, 57)),
                Tuple.Create("hsla(296, 85%, 12%, 90%)",   new Color(230, 53, 5, 57)),

                // HSV
                Tuple.Create("hsv(240, 83%, 78%)",         new Color(255, 34, 34, 199)),
                Tuple.Create("hsva(240, 0.83, 0.78, 0.9)", new Color(230, 34, 34, 199)),
                Tuple.Create("hsva(240, 83%, 78%, 90%)",   new Color(230, 34, 34, 199)),
            };

            foreach (var dataPoint in data)
            {
                Assert.True(Color.TryParse(dataPoint.Item1, out Color parsedColor));
                Assert.True(dataPoint.Item2 == parsedColor);
            }
        }

        [Fact]
        public void Hsv_To_From_Hsl_Conversion()
        {
            // Note that conversion of values more representative of actual colors is not done due to rounding error
            // It would be necessary to introduce a different equality comparison that accounts for rounding differences in values
            // This is a result of the math in the conversion itself
            // RGB doesn't have this problem because it uses whole numbers
            var data = new Tuple<HsvColor, HslColor>[]
            {
                Tuple.Create(new HsvColor(1.0, 0.0, 0.0, 0.0), new HslColor(1.0, 0.0, 0.0, 0.0)),
                Tuple.Create(new HsvColor(1.0, 359.0, 1.0, 1.0), new HslColor(1.0, 359.0, 1.0, 0.5)),

                Tuple.Create(new HsvColor(1.0, 128.0, 0.0, 0.0), new HslColor(1.0, 128.0, 0.0, 0.0)),
                Tuple.Create(new HsvColor(1.0, 128.0, 0.0, 1.0), new HslColor(1.0, 128.0, 0.0, 1.0)),
                Tuple.Create(new HsvColor(1.0, 128.0, 1.0, 1.0), new HslColor(1.0, 128.0, 1.0, 0.5)),

                Tuple.Create(new HsvColor(0.23, 0.5, 1.0, 1.0), new HslColor(0.23, 0.5, 1.0, 0.5)),
            };

            foreach (var dataPoint in data)
            {
                var convertedHsl = dataPoint.Item1.ToHsl();
                var convertedHsv = dataPoint.Item2.ToHsv();

                Assert.Equal(convertedHsv, dataPoint.Item1);
                Assert.Equal(convertedHsl, dataPoint.Item2);
            }
        }
        // =====================================================================
        // IFormattable unified format specifier tests
        //
        // All three color types (Color, HslColor, HsvColor) support ALL
        // format specifiers. Cross-model formats auto-convert.
        //
        // Convention:
        //   Uppercase = include alpha, a-suffixed prefix (rgba, hsla, hsva)
        //   Lowercase = exclude alpha, plain prefix (rgb, hsl, hsv)
        //   "%" suffix = percent mode
        // =====================================================================

        #region Default format (null / "")

        [Fact]
        public void Color_ToString_Default_Returns_KnownName()
        {
            var red = new Color(0xFF, 0xFF, 0x00, 0x00);

            Assert.Equal("Red", red.ToString());
        }

        [Fact]
        public void Color_ToString_Default_Returns_Hex_For_Unknown()
        {
            var color = new Color(0x40, 0xFF, 0x88, 0x44);

            Assert.Equal("#40ff8844", color.ToString());
        }

        [Fact]
        public void Color_ToString_Null_Format_Matches_Default()
        {
            var color = new Color(0x40, 0xFF, 0x88, 0x44);

            Assert.Equal(color.ToString(), color.ToString(null, null));
        }

        [Fact]
        public void Color_ToString_Empty_Format_Matches_Default()
        {
            var color = new Color(0x40, 0xFF, 0x88, 0x44);

            Assert.Equal(color.ToString(), color.ToString("", null));
        }

        [Fact]
        public void HslColor_ToString_Null_Format_Matches_Default()
        {
            var color = new HslColor(0.8, 200, 0.6, 0.4);

            Assert.Equal(color.ToString(), color.ToString(null, null));
        }

        [Fact]
        public void HsvColor_ToString_Null_Format_Matches_Default()
        {
            var color = new HsvColor(0.8, 200, 0.6, 0.4);

            Assert.Equal(color.ToString(), color.ToString(null, null));
        }

        #endregion

        #region Hex formats: X, x, H

        [Theory]
        [InlineData(0xFF, 0xFF, 0x88, 0x44, "#FFFF8844")]
        [InlineData(0x40, 0xFF, 0x88, 0x44, "#40FF8844")]
        [InlineData(0xFF, 0x00, 0x00, 0x00, "#FF000000")]
        [InlineData(0x00, 0x00, 0x00, 0x00, "#00000000")]
        public void Color_ToString_X_Returns_Xaml_Hex_With_Alpha(byte a, byte r, byte g, byte b, string expected)
        {
            var color = new Color(a, r, g, b);

            Assert.Equal(expected, color.ToString("X", CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData(0xFF, 0xFF, 0x88, 0x44, "#FF8844")]
        [InlineData(0x40, 0xFF, 0x88, 0x44, "#FF8844")]
        [InlineData(0x00, 0x00, 0x00, 0x00, "#000000")]
        public void Color_ToString_x_Returns_Hex_Without_Alpha(byte a, byte r, byte g, byte b, string expected)
        {
            var color = new Color(a, r, g, b);

            Assert.Equal(expected, color.ToString("x", CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData(0xFF, 0xFF, 0x88, 0x44, "#FF8844FF")]
        [InlineData(0x40, 0xFF, 0x88, 0x44, "#FF884440")]
        [InlineData(0x00, 0x00, 0x00, 0x00, "#00000000")]
        public void Color_ToString_H_Returns_Html_Hex_With_Alpha(byte a, byte r, byte g, byte b, string expected)
        {
            var color = new Color(a, r, g, b);

            Assert.Equal(expected, color.ToString("H", CultureInfo.InvariantCulture));
        }

        [Fact]
        public void HslColor_ToString_X_Converts_To_Rgb_Hex()
        {
            // Pure red: HSL(0, 1, 0.5) = RGB(255, 0, 0)
            var hsl = new HslColor(1.0, 0, 1.0, 0.5);

            Assert.Equal("#FFFF0000", hsl.ToString("X", CultureInfo.InvariantCulture));
        }

        [Fact]
        public void HslColor_ToString_x_Converts_To_Rgb_Hex()
        {
            var hsl = new HslColor(1.0, 0, 1.0, 0.5);

            Assert.Equal("#FF0000", hsl.ToString("x", CultureInfo.InvariantCulture));
        }

        [Fact]
        public void HsvColor_ToString_X_Converts_To_Rgb_Hex()
        {
            // Pure red: HSV(0, 1, 1) = RGB(255, 0, 0)
            var hsv = new HsvColor(1.0, 0, 1.0, 1.0);

            Assert.Equal("#FFFF0000", hsv.ToString("X", CultureInfo.InvariantCulture));
        }

        [Fact]
        public void HsvColor_ToString_x_Converts_To_Rgb_Hex()
        {
            var hsv = new HsvColor(1.0, 0, 1.0, 1.0);

            Assert.Equal("#FF0000", hsv.ToString("x", CultureInfo.InvariantCulture));
        }

        #endregion

        #region RGB functional: R, r

        [Theory]
        [InlineData(0xFF, 0xFF, 0x88, 0x44, "rgba(255, 136, 68, 1.00)")]
        [InlineData(0x80, 0xFF, 0x88, 0x44, "rgba(255, 136, 68, 0.50)")]
        [InlineData(0x00, 0x00, 0x00, 0x00, "rgba(0, 0, 0, 0.00)")]
        public void Color_ToString_R_Returns_Rgba_With_Alpha(byte a, byte r, byte g, byte b, string expected)
        {
            var color = new Color(a, r, g, b);

            Assert.Equal(expected, color.ToString("R", CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData(0xFF, 0xFF, 0x88, 0x44, "rgb(255, 136, 68)")]
        [InlineData(0x80, 0xFF, 0x88, 0x44, "rgb(255, 136, 68)")]
        [InlineData(0xFF, 0x00, 0x00, 0x00, "rgb(0, 0, 0)")]
        public void Color_ToString_r_Returns_Rgb_Without_Alpha(byte a, byte r, byte g, byte b, string expected)
        {
            var color = new Color(a, r, g, b);

            Assert.Equal(expected, color.ToString("r", CultureInfo.InvariantCulture));
        }

        [Fact]
        public void HslColor_ToString_R_Converts_To_Rgba()
        {
            // Pure blue: HSL(240, 1, 0.5) = RGB(0, 0, 255)
            var hsl = new HslColor(1.0, 240, 1.0, 0.5);

            Assert.Equal("rgba(0, 0, 255, 1.00)", hsl.ToString("R", CultureInfo.InvariantCulture));
        }

        [Fact]
        public void HsvColor_ToString_r_Converts_To_Rgb()
        {
            // Pure red: HSV(0, 1, 1) = RGB(255, 0, 0)
            var hsv = new HsvColor(1.0, 0, 1.0, 1.0);

            Assert.Equal("rgb(255, 0, 0)", hsv.ToString("r", CultureInfo.InvariantCulture));
        }

        #endregion

        #region RGB percent: R%, r%

        [Theory]
        [InlineData(0xFF, 0xFF, 0x80, 0x00, "rgba(100%, 50%, 0%, 100%)")]
        [InlineData(0x80, 0xFF, 0x80, 0x00, "rgba(100%, 50%, 0%, 50%)")]
        [InlineData(0x00, 0x00, 0x00, 0x00, "rgba(0%, 0%, 0%, 0%)")]
        public void Color_ToString_RPct_Returns_Rgba_Percent(byte a, byte r, byte g, byte b, string expected)
        {
            var color = new Color(a, r, g, b);

            Assert.Equal(expected, color.ToString("R%", CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData(0xFF, 0xFF, 0x80, 0x00, "rgb(100%, 50%, 0%)")]
        [InlineData(0x80, 0xFF, 0x80, 0x00, "rgb(100%, 50%, 0%)")]
        [InlineData(0xFF, 0x00, 0x00, 0x00, "rgb(0%, 0%, 0%)")]
        public void Color_ToString_rPct_Returns_Rgb_Percent(byte a, byte r, byte g, byte b, string expected)
        {
            var color = new Color(a, r, g, b);

            Assert.Equal(expected, color.ToString("r%", CultureInfo.InvariantCulture));
        }

        [Fact]
        public void HslColor_ToString_RPct_Converts_To_Rgba_Percent()
        {
            // Pure red: HSL(0, 1, 0.5) = RGB(255, 0, 0)
            var hsl = new HslColor(1.0, 0, 1.0, 0.5);

            Assert.Equal("rgba(100%, 0%, 0%, 100%)", hsl.ToString("R%", CultureInfo.InvariantCulture));
        }

        #endregion

        #region HSL functional: L, l

        [Theory]
        [InlineData(1.0, 180, 0.5, 0.5, "hsla(180, 50%, 50%, 1.00)")]
        [InlineData(0.5, 240, 0.8, 0.2, "hsla(240, 80%, 20%, 0.50)")]
        [InlineData(0.0, 0, 0.0, 0.0, "hsla(0, 0%, 0%, 0.00)")]
        public void HslColor_ToString_L_Returns_Hsla_With_Alpha(double a, double h, double s, double l, string expected)
        {
            var color = new HslColor(a, h, s, l);

            Assert.Equal(expected, color.ToString("L", CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData(1.0, 180, 0.5, 0.5, "hsl(180, 50%, 50%)")]
        [InlineData(0.5, 240, 0.8, 0.2, "hsl(240, 80%, 20%)")]
        [InlineData(0.0, 0, 0.0, 0.0, "hsl(0, 0%, 0%)")]
        public void HslColor_ToString_l_Returns_Hsl_Without_Alpha(double a, double h, double s, double l, string expected)
        {
            var color = new HslColor(a, h, s, l);

            Assert.Equal(expected, color.ToString("l", CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Color_ToString_L_Converts_To_Hsla()
        {
            // Pure red: RGB(255, 0, 0) = HSL(0, 100%, 50%)
            var color = new Color(0xFF, 0xFF, 0x00, 0x00);

            Assert.Equal("hsla(0, 100%, 50%, 1.00)", color.ToString("L", CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Color_ToString_l_Converts_To_Hsl()
        {
            var color = new Color(0xFF, 0xFF, 0x00, 0x00);

            Assert.Equal("hsl(0, 100%, 50%)", color.ToString("l", CultureInfo.InvariantCulture));
        }

        [Fact]
        public void HsvColor_ToString_L_Converts_To_Hsla()
        {
            // Pure red: HSV(0, 1, 1) = HSL(0, 100%, 50%)
            var hsv = new HsvColor(1.0, 0, 1.0, 1.0);

            Assert.Equal("hsla(0, 100%, 50%, 1.00)", hsv.ToString("L", CultureInfo.InvariantCulture));
        }

        #endregion

        #region HSL percent: L%, l%

        [Theory]
        [InlineData(1.0, 180, 0.5, 0.5, "hsla(50%, 50%, 50%, 100%)")]
        [InlineData(0.5, 90, 1.0, 1.0, "hsla(25%, 100%, 100%, 50%)")]
        [InlineData(1.0, 0, 0.0, 0.0, "hsla(0%, 0%, 0%, 100%)")]
        public void HslColor_ToString_LPct_Returns_Hsla_All_Percent(double a, double h, double s, double l, string expected)
        {
            var color = new HslColor(a, h, s, l);

            Assert.Equal(expected, color.ToString("L%", CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData(1.0, 180, 0.5, 0.5, "hsl(50%, 50%, 50%)")]
        [InlineData(0.5, 90, 1.0, 1.0, "hsl(25%, 100%, 100%)")]
        [InlineData(1.0, 0, 0.0, 0.0, "hsl(0%, 0%, 0%)")]
        public void HslColor_ToString_lPct_Returns_Hsl_All_Percent(double a, double h, double s, double l, string expected)
        {
            var color = new HslColor(a, h, s, l);

            Assert.Equal(expected, color.ToString("l%", CultureInfo.InvariantCulture));
        }

        #endregion

        #region HSV functional: V, v

        [Theory]
        [InlineData(1.0, 180, 0.5, 0.5, "hsva(180, 50%, 50%, 1.00)")]
        [InlineData(0.5, 240, 0.8, 0.2, "hsva(240, 80%, 20%, 0.50)")]
        [InlineData(0.0, 0, 0.0, 0.0, "hsva(0, 0%, 0%, 0.00)")]
        public void HsvColor_ToString_V_Returns_Hsva_With_Alpha(double a, double h, double s, double v, string expected)
        {
            var color = new HsvColor(a, h, s, v);

            Assert.Equal(expected, color.ToString("V", CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData(1.0, 180, 0.5, 0.5, "hsv(180, 50%, 50%)")]
        [InlineData(0.5, 240, 0.8, 0.2, "hsv(240, 80%, 20%)")]
        [InlineData(0.0, 0, 0.0, 0.0, "hsv(0, 0%, 0%)")]
        public void HsvColor_ToString_v_Returns_Hsv_Without_Alpha(double a, double h, double s, double v, string expected)
        {
            var color = new HsvColor(a, h, s, v);

            Assert.Equal(expected, color.ToString("v", CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Color_ToString_V_Converts_To_Hsva()
        {
            // Pure red: RGB(255, 0, 0) = HSV(0, 100%, 100%)
            var color = new Color(0xFF, 0xFF, 0x00, 0x00);

            Assert.Equal("hsva(0, 100%, 100%, 1.00)", color.ToString("V", CultureInfo.InvariantCulture));
        }

        [Fact]
        public void HslColor_ToString_V_Converts_To_Hsva()
        {
            // Pure red: HSL(0, 1, 0.5) = HSV(0, 100%, 100%)
            var hsl = new HslColor(1.0, 0, 1.0, 0.5);

            Assert.Equal("hsva(0, 100%, 100%, 1.00)", hsl.ToString("V", CultureInfo.InvariantCulture));
        }

        #endregion

        #region HSV percent: V%, v%

        [Theory]
        [InlineData(1.0, 180, 0.5, 0.5, "hsva(50%, 50%, 50%, 100%)")]
        [InlineData(0.5, 90, 1.0, 1.0, "hsva(25%, 100%, 100%, 50%)")]
        [InlineData(1.0, 0, 0.0, 0.0, "hsva(0%, 0%, 0%, 100%)")]
        public void HsvColor_ToString_VPct_Returns_Hsva_All_Percent(double a, double h, double s, double v, string expected)
        {
            var color = new HsvColor(a, h, s, v);

            Assert.Equal(expected, color.ToString("V%", CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData(1.0, 180, 0.5, 0.5, "hsv(50%, 50%, 50%)")]
        [InlineData(0.5, 90, 1.0, 1.0, "hsv(25%, 100%, 100%)")]
        [InlineData(1.0, 0, 0.0, 0.0, "hsv(0%, 0%, 0%)")]
        public void HsvColor_ToString_vPct_Returns_Hsv_All_Percent(double a, double h, double s, double v, string expected)
        {
            var color = new HsvColor(a, h, s, v);

            Assert.Equal(expected, color.ToString("v%", CultureInfo.InvariantCulture));
        }

        #endregion

        #region Invalid format + reserved specifiers

        [Fact]
        public void Color_ToString_Invalid_Format_Throws()
        {
            var color = new Color(0xFF, 0xFF, 0x00, 0x00);

            Assert.Throws<FormatException>(() => color.ToString("Z", null));
        }

        [Fact]
        public void HslColor_ToString_Invalid_Format_Throws()
        {
            var color = new HslColor(1.0, 0, 0, 0);

            Assert.Throws<FormatException>(() => color.ToString("Z", null));
        }

        [Fact]
        public void HsvColor_ToString_Invalid_Format_Throws()
        {
            var color = new HsvColor(1.0, 0, 0, 0);

            Assert.Throws<FormatException>(() => color.ToString("Z", null));
        }

        [Theory]
        [InlineData("C")]
        [InlineData("c")]
        [InlineData("A")]
        [InlineData("a")]
        [InlineData("P")]
        [InlineData("h")]
        public void Color_ToString_Reserved_And_Removed_Specifiers_Throw(string format)
        {
            var color = new Color(0xFF, 0xFF, 0x00, 0x00);

            Assert.Throws<FormatException>(() => color.ToString(format, null));
        }

        [Theory]
        [InlineData("C")]
        [InlineData("c")]
        public void HslColor_ToString_Reserved_C_Throws(string format)
        {
            var color = new HslColor(1.0, 0, 1.0, 0.5);

            Assert.Throws<FormatException>(() => color.ToString(format, null));
        }

        [Theory]
        [InlineData("C")]
        [InlineData("c")]
        public void HsvColor_ToString_Reserved_C_Throws(string format)
        {
            var color = new HsvColor(1.0, 0, 1.0, 1.0);

            Assert.Throws<FormatException>(() => color.ToString(format, null));
        }

        #endregion

        #region IFormatProvider is always ignored (culture-invariant)

        [Fact]
        public void Color_ToString_IFormatProvider_Is_Ignored()
        {
            var color = new Color(0x80, 0xFF, 0x88, 0x44);
            var french = CultureInfo.GetCultureInfo("fr-FR");

            Assert.Equal(
                color.ToString("R", CultureInfo.InvariantCulture),
                color.ToString("R", french));
        }

        [Fact]
        public void HslColor_ToString_IFormatProvider_Is_Ignored()
        {
            var color = new HslColor(0.5, 180, 0.5, 0.5);
            var french = CultureInfo.GetCultureInfo("fr-FR");

            Assert.Equal(
                color.ToString("L", CultureInfo.InvariantCulture),
                color.ToString("L", french));
        }

        #endregion
    }
}
