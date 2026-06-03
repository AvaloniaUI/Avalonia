using System;
using Avalonia.Media;
using Avalonia.Skia;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;
using SkiaSharp;

namespace Avalonia.Benchmarks.Media.Variation
{
    /// <summary>
    /// Measures per-glyph bounding-box reads. Unlike <see cref="AdvanceLookupBenchmark"/>,
    /// this code path <b>is</b> in the common text rendering hot path —
    /// <c>GlyphRunImpl</c> calls <c>SKFont.GetGlyphWidths(_glyphIndices, null,
    /// glyphBounds)</c> on every render to fill the per-glyph bounds array, which feeds
    /// run-bounds computation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Skia baseline is the actual production call. Our variant reads the bbox
    /// directly from the <c>glyf</c> entry header (4 <c>int16</c> reads per glyph after
    /// the <c>loca</c> lookup). If the gap is meaningful, rewiring <c>GlyphRunImpl</c> to
    /// use our reader is a concrete rendering-pipeline optimization.
    /// </para>
    /// <para>
    /// Caveats on the comparison:
    /// </para>
    /// <list type="bullet">
    /// <item>Our reader returns design-space bounds (font design units). Skia's
    /// <c>GetGlyphWidths</c> bounds are in rendered pixels — they factor in font size,
    /// hinting, edging, and subpixel snapping. <c>GlyphRunImpl</c> uses the rendered
    /// bounds for run-bounds union; if we replace it we'd need to either (a) scale
    /// our design-space bounds to pixels (cheap, loses hinting offsets) or
    /// (b) accept a small precision loss in run bounds.</item>
    /// <item>Our reader uses the <c>glyf</c> header bbox, which is the default
    /// instance for variable fonts. Variation deformation (gvar) shifts the bbox;
    /// computing the deformed bbox requires reading the variation deltas, which we
    /// haven't wired here.</item>
    /// </list>
    /// </remarks>
    [MemoryDiagnoser]
    [MinIterationTime(250)]
    [MaxWarmupCount(20)]
    public class BoundsLookupBenchmark : IDisposable
    {
        private readonly IDisposable _app;

        private GlyphTypeface _static = null!;
        private ushort _staticGlyph;
        private ushort[] _glyphs20 = null!;
        private ushort[] _glyphs200 = null!;
        private ushort[] _glyphs2000 = null!;

        // Output buffers sized for the largest batch.
        private short[] _xMins = null!;
        private short[] _yMins = null!;
        private short[] _xMaxs = null!;
        private short[] _yMaxs = null!;

        // Skia baseline. _skBounds matches what GlyphRunImpl passes today.
        private SKFont _skFont = null!;
        private float[] _skWidths = null!;
        private SKRect[] _skBounds = null!;

        public BoundsLookupBenchmark()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _static = VariationFixtures.LoadInterRegular();
            _staticGlyph = _static.CharacterToGlyphMap['e'];

            _glyphs20 = VariationFixtures.BuildWordGlyphs(_static);
            _glyphs200 = VariationFixtures.BuildLineGlyphs(_static);
            _glyphs2000 = VariationFixtures.BuildParagraphGlyphs(_static);

            _xMins = new short[2000];
            _yMins = new short[2000];
            _xMaxs = new short[2000];
            _yMaxs = new short[2000];

            var skTypeface = ((SkiaTypeface)_static.PlatformTypeface).SKTypeface;
            _skFont = new SKFont(skTypeface, size: 16);
            _skWidths = new float[2000];
            _skBounds = new SKRect[2000];
        }

        // ----- Ours: direct glyf-header read -----

        [Benchmark]
        public bool Glyf_Single()
            => _static.TryGetGlyphBounds(_staticGlyph, out _, out _, out _, out _);

        [Benchmark]
        public bool Glyf_Batch20()
            => _static.TryGetGlyphBounds(_glyphs20,
                _xMins.AsSpan(0, 20), _yMins.AsSpan(0, 20),
                _xMaxs.AsSpan(0, 20), _yMaxs.AsSpan(0, 20));

        [Benchmark(Baseline = true)]
        public bool Glyf_Batch200()
            => _static.TryGetGlyphBounds(_glyphs200,
                _xMins.AsSpan(0, 200), _yMins.AsSpan(0, 200),
                _xMaxs.AsSpan(0, 200), _yMaxs.AsSpan(0, 200));

        [Benchmark]
        public bool Glyf_Batch2000()
            => _static.TryGetGlyphBounds(_glyphs2000,
                _xMins.AsSpan(), _yMins.AsSpan(),
                _xMaxs.AsSpan(), _yMaxs.AsSpan());

        // ----- Skia baseline: matches the production GlyphRunImpl call -----

        /// <summary>
        /// The exact shape of the call GlyphRunImpl makes today: widths span ignored
        /// (null in SkiaSharp = default span), bounds requested. This is the
        /// rendering-path number we're trying to beat.
        /// </summary>
        [Benchmark]
        public void Skia_BoundsOnly_Batch200()
        {
            _skFont.GetGlyphWidths(_glyphs200.AsSpan(),
                widths: default,
                _skBounds.AsSpan(0, 200));
        }

        /// <summary>
        /// Skia widths-only as a control — anything beyond this number is what Skia
        /// pays specifically for bounds.
        /// </summary>
        [Benchmark]
        public void Skia_WidthsOnly_Batch200()
        {
            _skFont.GetGlyphWidths(_glyphs200.AsSpan(),
                _skWidths.AsSpan(0, 200),
                bounds: default);
        }

        public void Dispose()
        {
            _skFont?.Dispose();
            _app?.Dispose();
        }
    }
}
