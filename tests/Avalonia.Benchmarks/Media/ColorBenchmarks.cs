using System;
using Avalonia.Media;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Media
{
    [MemoryDiagnoser]
    public class ColorBenchmarks
    {
        private string _hexColor = "#FF5733";
        private string _rgbColor = "rgb(255, 87, 51)";
        private string _rgbaColor = "rgba(255, 87, 51, 0.8)";
        private string _namedColor = "Red";
        private Color _color;

        [GlobalSetup]
        public void Setup()
        {
            _color = Colors.Red;
        }

        /// <summary>
        /// Benchmark parsing hex color
        /// </summary>
        [Benchmark(Baseline = true)]
        public Color ParseHexColor()
        {
            return Color.Parse(_hexColor);
        }

        /// <summary>
        /// Benchmark parsing rgb color
        /// </summary>
        [Benchmark]
        public Color ParseRgbColor()
        {
            return Color.Parse(_rgbColor);
        }

        /// <summary>
        /// Benchmark parsing rgba color
        /// </summary>
        [Benchmark]
        public Color ParseRgbaColor()
        {
            return Color.Parse(_rgbaColor);
        }

        /// <summary>
        /// Benchmark parsing named color
        /// </summary>
        [Benchmark]
        public Color ParseNamedColor()
        {
            return Color.Parse(_namedColor);
        }

        /// <summary>
        /// Benchmark TryParse (common pattern)
        /// </summary>
        [Benchmark]
        public bool TryParseColor()
        {
            return Color.TryParse(_hexColor, out _);
        }

        /// <summary>
        /// Benchmark color to HSL conversion
        /// </summary>
        [Benchmark]
        public HslColor ToHsl()
        {
            return _color.ToHsl();
        }

        /// <summary>
        /// Benchmark color to HSV conversion
        /// </summary>
        [Benchmark]
        public HsvColor ToHsv()
        {
            return _color.ToHsv();
        }

        /// <summary>
        /// Benchmark HSL to Color conversion
        /// </summary>
        [Benchmark]
        public Color FromHsl()
        {
            return new HslColor(1.0, 0.5, 0.5, 0.5).ToRgb();
        }

        /// <summary>
        /// Benchmark color string representation
        /// </summary>
        [Benchmark]
        public string ColorToString()
        {
            return _color.ToString();
        }

        /// <summary>
        /// Benchmark getting color from known brush (public API)
        /// </summary>
        [Benchmark]
        public ISolidColorBrush? KnownBrushLookup()
        {
            return Brushes.Red;
        }
    }
}
