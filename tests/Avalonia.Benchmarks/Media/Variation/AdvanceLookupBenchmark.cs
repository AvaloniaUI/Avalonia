using System;
using Avalonia.Media;
using Avalonia.Skia;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;
using SkiaSharp;

namespace Avalonia.Benchmarks.Media.Variation
{
    /// <summary>
    /// Headline benchmark for the per-glyph hot path. Measures
    /// <see cref="GlyphTypeface.TryGetHorizontalGlyphAdvance"/> (single) and
    /// <see cref="GlyphTypeface.TryGetHorizontalGlyphAdvances"/> (batch) across three
    /// typeface modes (static / variable-default / variable-varied) and four batch
    /// sizes (1 / 20 / 200 / 2000).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each per-glyph operation goes through here in real text layout — every
    /// <c>DrawText</c>, <c>FormattedText</c>, and <c>TextBlock</c> call eventually walks
    /// the batch advance path. Regressions multiply across paragraph-size inputs.
    /// </para>
    /// <para>
    /// Hypotheses the variants are designed to test:
    /// </para>
    /// <list type="number">
    /// <item><b>H1</b> Variable-default within 5% of static — the null-table
    /// fast-path check is effectively free.</item>
    /// <item><b>H2</b> Variable-varied within 50% of static per glyph — HVAR
    /// delta + region scaler + clamp is the only added work.</item>
    /// <item><b>H3</b> Batch path is linear in glyph count for all modes.</item>
    /// <item><b>H4</b> Within 2× of Skia's native <c>GetGlyphWidths</c>.</item>
    /// </list>
    /// </remarks>
    [MemoryDiagnoser]
    // The defaults of 150ms / 15 warmups produced ~0.1ns std dev on the sub-2ns rows
    // (Static_Single, VariableDefault_Single) only with --warmupCount 10
    // --iterationCount 20 overrides. Bumping the in-class defaults so the standard
    // BenchmarkSwitcher invocation already lands in a stable region without command-line
    // overrides; the cost is ~90s instead of ~30s per run.
    [MinIterationTime(250)]
    [MaxWarmupCount(20)]
    public class AdvanceLookupBenchmark : IDisposable
    {
        private readonly IDisposable _app;

        private GlyphTypeface _static = null!;
        private GlyphTypeface _variableDefault = null!;
        private GlyphTypeface _variableVaried = null!;

        // Single-glyph inputs (one common glyph from each typeface). Stored so the
        // Single benchmarks don't pay an array-index per call.
        private ushort _staticGlyph;
        private ushort _variableGlyph;

        // Batch inputs at 20 / 200 / 2000. Same content across the three typeface
        // modes because Inter-Regular and Inter Variable share the same cmap for
        // Latin code points; the GIDs land at the same positions.
        private ushort[] _glyphs20 = null!;
        private ushort[] _glyphs200 = null!;
        private ushort[] _glyphs2000 = null!;

        // Output buffers, sized to the largest batch so we never reallocate inside
        // a benchmark iteration.
        private ushort[] _advances2000 = null!;

        // Skia baseline: an SKFont over the same Inter Variable typeface. Used by the
        // Skia_Batch200 baseline benchmark for the "are we close to native Skia perf"
        // question.
        private SKFont _skFont = null!;
        private SKRect[] _skBounds2000 = null!;

        public AdvanceLookupBenchmark()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _static = VariationFixtures.LoadInterRegular();
            _variableDefault = VariationFixtures.LoadInterVariable();
            _variableVaried = _variableDefault.WithVariation(
                VariationFixtures.WghtSettings(_variableDefault, 900f));

            _staticGlyph = _static.CharacterToGlyphMap['e'];
            _variableGlyph = _variableDefault.CharacterToGlyphMap['e'];

            // Build batch inputs against the static typeface and reuse — the cmap
            // matches for both fonts on the ASCII chars in the sample.
            _glyphs20 = VariationFixtures.BuildWordGlyphs(_static);
            _glyphs200 = VariationFixtures.BuildLineGlyphs(_static);
            _glyphs2000 = VariationFixtures.BuildParagraphGlyphs(_static);

            _advances2000 = new ushort[2000];

            // Skia baseline: pull the SKTypeface back out of the variable Inter
            // typeface's platform impl and create an SKFont over it.
            var skTypeface = ((SkiaTypeface)_variableDefault.PlatformTypeface).SKTypeface;
            _skFont = new SKFont(skTypeface, size: 16);
            _skBounds2000 = new SKRect[2000];
        }

        // ----- Single-glyph variants -----

        [Benchmark]
        public bool Static_Single()
            => _static.TryGetHorizontalGlyphAdvance(_staticGlyph, out _);

        [Benchmark]
        public bool VariableDefault_Single()
            => _variableDefault.TryGetHorizontalGlyphAdvance(_variableGlyph, out _);

        [Benchmark]
        public bool VariableVaried_Single()
            => _variableVaried.TryGetHorizontalGlyphAdvance(_variableGlyph, out _);

        // ----- Batch 20 (word-size) -----

        [Benchmark]
        public bool StaticDefault_Batch20()
            => _static.TryGetHorizontalGlyphAdvances(_glyphs20, _advances2000.AsSpan(0, 20));

        [Benchmark]
        public bool VariableDefault_Batch20()
            => _variableDefault.TryGetHorizontalGlyphAdvances(_glyphs20, _advances2000.AsSpan(0, 20));

        [Benchmark]
        public bool VariableVaried_Batch20()
            => _variableVaried.TryGetHorizontalGlyphAdvances(_glyphs20, _advances2000.AsSpan(0, 20));

        // ----- Batch 200 (line-size — primary comparison point) -----

        [Benchmark(Baseline = true)]
        public bool StaticDefault_Batch200()
            => _static.TryGetHorizontalGlyphAdvances(_glyphs200, _advances2000.AsSpan(0, 200));

        [Benchmark]
        public bool VariableDefault_Batch200()
            => _variableDefault.TryGetHorizontalGlyphAdvances(_glyphs200, _advances2000.AsSpan(0, 200));

        [Benchmark]
        public bool VariableVaried_Batch200()
            => _variableVaried.TryGetHorizontalGlyphAdvances(_glyphs200, _advances2000.AsSpan(0, 200));

        // ----- Batch 2000 (long paragraph / linearity check) -----

        [Benchmark]
        public bool StaticDefault_Batch2000()
            => _static.TryGetHorizontalGlyphAdvances(_glyphs2000, _advances2000);

        [Benchmark]
        public bool VariableDefault_Batch2000()
            => _variableDefault.TryGetHorizontalGlyphAdvances(_glyphs2000, _advances2000);

        [Benchmark]
        public bool VariableVaried_Batch2000()
            => _variableVaried.TryGetHorizontalGlyphAdvances(_glyphs2000, _advances2000);

        // ----- Skia native baseline -----

        /// <summary>
        /// Skia's native batch read at 200 glyphs, asking for both widths and bounds.
        /// This matches what GlyphRunImpl actually calls during rendering — Avalonia's
        /// hot text path uses GetGlyphWidths for the per-glyph bounding boxes that
        /// feed run-bounds computation. Per glyph this is meaningfully more work than
        /// our hmtx-only TryGetHorizontalGlyphAdvances.
        /// </summary>
        [Benchmark]
        public void Skia_Batch200_WidthsAndBounds()
        {
            _skFont.GetGlyphWidths(_glyphs200.AsSpan(), _advances2000.AsSpan(0, 200).Slice(0).AsFloatSpan(),
                _skBounds2000.AsSpan(0, 200));
        }

        /// <summary>
        /// Skia's native batch read at 200 glyphs, asking for widths only — the
        /// like-for-like comparison against our TryGetHorizontalGlyphAdvances. Skia's
        /// GetGlyphWidths still goes through SKFont (which respects edging / hinting
        /// / subpixel settings), so this isn't a pure hmtx read on its side either —
        /// but it's the closest apples-to-apples comparison the public API offers.
        /// </summary>
        [Benchmark]
        public void Skia_Batch200_WidthsOnly()
        {
            _skFont.GetGlyphWidths(_glyphs200.AsSpan(), _advances2000.AsSpan(0, 200).Slice(0).AsFloatSpan(),
                bounds: default);
        }

        public void Dispose()
        {
            _skFont?.Dispose();
            _app?.Dispose();
        }
    }

    /// <summary>
    /// Bridge from <see cref="Span{T}"/> of <see cref="ushort"/> to the float span
    /// SkiaSharp's <c>GetGlyphWidths</c> needs. We don't actually use the float values
    /// — the benchmark just measures Skia's traversal cost.
    /// </summary>
    file static class FloatSpanBridge
    {
        public static Span<float> AsFloatSpan(this Span<ushort> span)
            => System.Runtime.InteropServices.MemoryMarshal.Cast<ushort, float>(span);
    }
}
