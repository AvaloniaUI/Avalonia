using System;
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
            Assert.Throws<ArgumentNullException>(() => Color.Parse((string)null));
        }

        [Fact]
        public void Parse_Throws_FormatException_For_Invalid_Input()
        {
            Assert.Throws<FormatException>(() => Color.Parse(string.Empty));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void TryParse_Returns_False_For_Invalid_Input(string input)
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
    }
}
