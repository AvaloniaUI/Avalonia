using System;
using Avalonia.Controls;
using Avalonia.Media;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Media
{
    /// <summary>
    /// Benchmarks for string parsing operations that use Split and allocate arrays.
    /// </summary>
    [MemoryDiagnoser]
    public class StringParsingBenchmarks
    {
        private const string SingleClass = "button";
        private const string ThreeClasses = "button primary active";
        private const string ManyClasses = "button primary active disabled selected highlighted focused hovered pressed";
        
        private const string SimpleBoxShadow = "5 5 10 #000000";
        private const string InsetBoxShadow = "inset 2 2 4 2 rgba(0,0,0,0.5)";
        private const string ComplexBoxShadow = "5 5 10 5 #ff0000";
        
        private const string MultipleBoxShadows = "5 5 10 #000000, inset 2 2 4 rgba(0,0,0,0.3)";
        
        private const string SimpleUnicodeRange = "U+0000-007F";
        private const string ComplexUnicodeRange = "U+0000-007F, U+0080-00FF, U+0100-017F, U+0180-024F";
        
        private const string SimpleFontFamily = "Arial";
        private const string FontFamilyWithFallback = "Segoe UI, Arial, sans-serif";
        
        private const string RgbColor = "rgb(255, 128, 64)";
        private const string RgbaColor = "rgba(255, 128, 64, 0.5)";
        
        private const string HslColorString = "hsl(180, 50%, 50%)";
        private const string HslaColorString = "hsla(180, 50%, 50%, 0.8)";

        [Benchmark(Baseline = true)]
        public Classes ParseClasses_Single()
        {
            return Classes.Parse(SingleClass);
        }

        [Benchmark]
        public Classes ParseClasses_Three()
        {
            return Classes.Parse(ThreeClasses);
        }

        [Benchmark]
        public Classes ParseClasses_Many()
        {
            return Classes.Parse(ManyClasses);
        }

        [Benchmark]
        public BoxShadow ParseBoxShadow_Simple()
        {
            return BoxShadow.Parse(SimpleBoxShadow);
        }

        [Benchmark]
        public BoxShadow ParseBoxShadow_Inset()
        {
            return BoxShadow.Parse(InsetBoxShadow);
        }

        [Benchmark]
        public BoxShadow ParseBoxShadow_Complex()
        {
            return BoxShadow.Parse(ComplexBoxShadow);
        }

        [Benchmark]
        public BoxShadows ParseBoxShadows_Multiple()
        {
            return BoxShadows.Parse(MultipleBoxShadows);
        }

        [Benchmark]
        public UnicodeRange ParseUnicodeRange_Simple()
        {
            return UnicodeRange.Parse(SimpleUnicodeRange);
        }

        [Benchmark]
        public UnicodeRange ParseUnicodeRange_Complex()
        {
            return UnicodeRange.Parse(ComplexUnicodeRange);
        }

        [Benchmark]
        public FontFamily ParseFontFamily_Simple()
        {
            return FontFamily.Parse(SimpleFontFamily);
        }

        [Benchmark]
        public FontFamily ParseFontFamily_WithFallback()
        {
            return FontFamily.Parse(FontFamilyWithFallback);
        }

        [Benchmark]
        public Color ParseColor_Rgb()
        {
            return Color.Parse(RgbColor);
        }

        [Benchmark]
        public Color ParseColor_Rgba()
        {
            return Color.Parse(RgbaColor);
        }

        [Benchmark]
        public HslColor ParseHslColor()
        {
            return HslColor.Parse(HslColorString);
        }

        [Benchmark]
        public HslColor ParseHslaColor()
        {
            return HslColor.Parse(HslaColorString);
        }
    }
}
