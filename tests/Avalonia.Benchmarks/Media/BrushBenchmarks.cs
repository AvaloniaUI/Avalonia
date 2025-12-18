using Avalonia.Media;
using Avalonia.Media.Immutable;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Media
{
    /// <summary>
    /// Benchmarks for Brush parsing and creation - brushes are fundamental
    /// for all rendering operations.
    /// </summary>
    [MemoryDiagnoser]
    public class BrushBenchmarks
    {
        private const string HexColor = "#FF5733";
        private const string NamedColor = "Red";
        private const string RgbColor = "rgb(255, 87, 51)";
        private const string RgbaColor = "rgba(255, 87, 51, 0.8)";
        
        private static readonly Color s_color = Colors.Red;
        private static readonly ISolidColorBrush s_solidBrush = new SolidColorBrush(Colors.Blue);

        /// <summary>
        /// Benchmark parsing brush from hex color
        /// </summary>
        [Benchmark(Baseline = true)]
        public IBrush ParseBrush_HexColor()
        {
            return Brush.Parse(HexColor);
        }

        /// <summary>
        /// Benchmark parsing brush from named color (uses cached known brush)
        /// </summary>
        [Benchmark]
        public IBrush ParseBrush_NamedColor()
        {
            return Brush.Parse(NamedColor);
        }

        /// <summary>
        /// Benchmark parsing brush from rgb() format
        /// </summary>
        [Benchmark]
        public IBrush ParseBrush_RgbColor()
        {
            return Brush.Parse(RgbColor);
        }

        /// <summary>
        /// Benchmark parsing brush from rgba() format
        /// </summary>
        [Benchmark]
        public IBrush ParseBrush_RgbaColor()
        {
            return Brush.Parse(RgbaColor);
        }

        /// <summary>
        /// Benchmark creating SolidColorBrush from Color
        /// </summary>
        [Benchmark]
        public SolidColorBrush CreateSolidColorBrush()
        {
            return new SolidColorBrush(s_color);
        }

        /// <summary>
        /// Benchmark creating ImmutableSolidColorBrush from Color
        /// </summary>
        [Benchmark]
        public ImmutableSolidColorBrush CreateImmutableSolidColorBrush()
        {
            return new ImmutableSolidColorBrush(s_color);
        }

        /// <summary>
        /// Benchmark getting known brush (Brushes.Red etc)
        /// </summary>
        [Benchmark]
        public ISolidColorBrush GetKnownBrush()
        {
            return Brushes.Red;
        }

        /// <summary>
        /// Benchmark brush opacity property access
        /// </summary>
        [Benchmark]
        public double GetBrushOpacity()
        {
            return s_solidBrush.Opacity;
        }

        /// <summary>
        /// Benchmark brush color property access
        /// </summary>
        [Benchmark]
        public Color GetBrushColor()
        {
            return s_solidBrush.Color;
        }
    }
}
