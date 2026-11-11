using System;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Skia;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;
using SkiaSharp;

namespace Avalonia.Benchmarks.Media.Variation
{
    /// <summary>
    /// The variation-clone lookup every text layout with font variations pays per run:
    /// <c>FontManager.TryGetGlyphTypeface</c> asks the default-instance typeface for the clone at
    /// the requested position on every call.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><c>Hit</c>: a held clone among the recently used ones, the steady state of static text.</item>
    /// <item><c>Hit_Settings</c>: the same through user-space settings, which adds normalization.</item>
    /// <item><c>Hit_Cycle32Held</c>: 32 held clones requested round robin, more than the cache holds
    /// strongly, so every request finds a clone outside the recently used set.</item>
    /// </list>
    /// </remarks>
    [MemoryDiagnoser]
    public class VariationCloneLookupBenchmark : IDisposable
    {
        private const int HeldClones = 32;

        private readonly IDisposable _app;

        private GlyphTypeface _source = null!;
        private NormalizedVariationPosition _bold;
        private FontVariationSettings _boldSettings = null!;
        private NormalizedVariationPosition[] _heldPositions = null!;
        private GlyphTypeface[] _held = null!;
        private int _next;

        public VariationCloneLookupBenchmark()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _source = VariationFixtures.LoadInterVariable();
            _bold = VariationFixtures.WghtPosition(_source, 700);
            _boldSettings = new FontVariationSettings(new[] { new FontVariation(VariationFixtures.WghtTag, 700) });

            _heldPositions = new NormalizedVariationPosition[HeldClones];
            _held = new GlyphTypeface[HeldClones + 1];

            for (var i = 0; i < HeldClones; i++)
            {
                _heldPositions[i] = VariationFixtures.WghtPosition(_source, 101 + (i * 25));
                _held[i] = _source.WithVariation(_heldPositions[i]);
            }

            _held[HeldClones] = _source.WithVariation(_bold);
        }

        [Benchmark(Baseline = true)]
        public GlyphTypeface Hit() => _source.WithVariation(_bold);

        [Benchmark]
        public GlyphTypeface Hit_Settings() => _source.WithVariations(_boldSettings);

        [Benchmark]
        public GlyphTypeface Hit_Cycle32Held()
        {
            var index = _next;
            _next = index + 1 == HeldClones ? 0 : index + 1;

            return _source.WithVariation(_heldPositions[index]);
        }

        public void Dispose() => _app.Dispose();
    }

    /// <summary>
    /// An animated axis: every frame asks for a position no frame asked for before, so each
    /// request creates a clone. One operation is one second of a 60 fps weight animation.
    /// </summary>
    /// <remarks>
    /// Each iteration starts from a fresh default-instance typeface, and its invocations walk on
    /// through the axis, so no position repeats within an iteration and both cache designs take
    /// the miss path on every request.
    /// </remarks>
    [MemoryDiagnoser]
    [InvocationCount(16)]
    public class VariationCloneAnimationBenchmark : IDisposable
    {
        private const int FramesPerSecond = 60;

        private readonly IDisposable _app;

        private GlyphTypeface _source = null!;
        private int _frame;

        public VariationCloneAnimationBenchmark()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);
        }

        [IterationSetup]
        public void IterationSetup()
        {
            _source = VariationFixtures.LoadInterVariable();
            _frame = 0;
        }

        [Benchmark]
        public GlyphTypeface OneSecondOfFrames()
        {
            GlyphTypeface last = _source;

            for (var i = 0; i < FramesPerSecond; i++, _frame++)
            {
                // 0.75 user units per frame stays far above the F2Dot14 quantization step of the
                // 100-900 axis (about 0.03 units), so every frame is a distinct position, and the
                // 960 frames of an iteration never land on the default at 400.
                last = _source.WithVariation(VariationFixtures.WghtPosition(_source, 101 + (_frame * 0.75)));
            }

            return last;
        }

        public void Dispose() => _app.Dispose();
    }

    /// <summary>
    /// Outline lookups through the glyph cache of a variation clone, the path a glyph run drawn
    /// from outlines takes per glyph, with Skia's own glyph path as the reference arm.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><c>Hit_*</c>: warm outlines of one 200-glyph line, at the default instance and at a clone.</item>
    /// <item><c>Bounds_Varied_Line</c>: ink bounds of the line at the clone, which read the same cache.</item>
    /// <item><c>Line_FiveInstances</c>: the line at five instances, text in several weights at once.</item>
    /// <item><c>FullSet_FiveInstances</c>: every outline of the font at five instances, about 2.9 MB
    /// of outlines per instance. Each instance's cache holds its whole set, so the steady state is
    /// all hits; a cache budget shared across instances would rebuild them instead.</item>
    /// </list>
    /// </remarks>
    [MemoryDiagnoser]
    public class VariationGlyphCacheBenchmark : IDisposable
    {
        private readonly IDisposable _app;

        private GlyphTypeface _source = null!;
        private GlyphTypeface _varied = null!;
        private GlyphTypeface[] _instances = null!;
        private ushort[] _line = null!;
        private GlyphBounds[] _bounds = null!;
        private SKFont _skFont = null!;

        public VariationGlyphCacheBenchmark()
        {
            _app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(
                renderInterface: new PlatformRenderInterface(),
                fontManagerImpl: new FontManagerImpl()));
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _source = VariationFixtures.LoadInterVariable();
            _varied = _source.WithVariation(VariationFixtures.WghtPosition(_source, 700));
            _instances = new[]
            {
                _source,
                _source.WithVariation(VariationFixtures.WghtPosition(_source, 100)),
                _source.WithVariation(VariationFixtures.WghtPosition(_source, 300)),
                _varied,
                _source.WithVariation(VariationFixtures.WghtPosition(_source, 900)),
            };

            _line = VariationFixtures.BuildLineGlyphs(_source);
            _bounds = new GlyphBounds[_line.Length];
            _skFont = ((SkiaTypeface)_source.PlatformTypeface).CreateSKFont(16f);

            Line_FiveInstances();
            Bounds_Varied_Line();
        }

        [Benchmark(Baseline = true)]
        public int Hit_Default_Line() => WalkLine(_source);

        [Benchmark]
        public int Hit_Varied_Line() => WalkLine(_varied);

        [Benchmark]
        public bool Bounds_Varied_Line() => _varied.TryGetGlyphBounds(_line, _bounds);

        [Benchmark]
        public int Skia_GlyphPath_Line()
        {
            var built = 0;

            foreach (var glyph in _line)
            {
                using var path = _skFont.GetGlyphPath(glyph);
                built += path is null ? 0 : 1;
            }

            return built;
        }

        [Benchmark]
        public int Line_FiveInstances()
        {
            var built = 0;

            foreach (var instance in _instances)
            {
                built += WalkLine(instance);
            }

            return built;
        }

        [Benchmark]
        public int FullSet_FiveInstances()
        {
            var built = 0;

            foreach (var instance in _instances)
            {
                for (var glyph = 0; glyph < instance.GlyphCount; glyph++)
                {
                    built += instance.GetGlyphOutline((ushort)glyph) is null ? 0 : 1;
                }
            }

            return built;
        }

        private int WalkLine(GlyphTypeface typeface)
        {
            var built = 0;

            foreach (var glyph in _line)
            {
                built += typeface.GetGlyphOutline(glyph) is null ? 0 : 1;
            }

            return built;
        }

        public void Dispose()
        {
            _skFont.Dispose();
            _app.Dispose();
        }
    }
}
