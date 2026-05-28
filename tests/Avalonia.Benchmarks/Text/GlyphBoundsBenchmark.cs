using System;
using System.Buffers;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Skia;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;
using SkiaSharp;

namespace Avalonia.Benchmarks.Text;

/// <summary>
/// Compares obtaining glyph ink bounds via the table-based
/// <see cref="GlyphTypeface.TryGetGlyphMetrics(ReadOnlySpan{ushort}, Span{GlyphMetrics})"/>
/// against Skia's <c>SKFont.GetGlyphWidths(..., bounds)</c> — the path
/// <c>GlyphRunImpl</c> uses today. Two scenario pairs isolate the pure bounds-read
/// cost (font pre-created) from the realistic per-run cost (Skia pays SKFont creation,
/// which the table path avoids entirely).
/// </summary>
[MemoryDiagnoser]
public class GlyphBoundsBenchmark : IDisposable
{
    private const float Size = 16f;

    private readonly IDisposable _app;
    private readonly GlyphTypeface _glyphTypeface;
    private readonly SkiaTypeface _skiaTypeface;
    private readonly SKFont _font;
    private readonly ushort[] _glyphPool;

    private ushort[] _glyphIds = Array.Empty<ushort>();
    private SKRect[] _skBounds = Array.Empty<SKRect>();
    private GlyphMetrics[] _metrics = Array.Empty<GlyphMetrics>();

    [Params(1, 16, 256)]
    public int GlyphCount { get; set; }

    public GlyphBoundsBenchmark()
    {
        _app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(
            renderInterface: new PlatformRenderInterface(),
            fontManagerImpl: new FontManagerImpl()));

        _glyphTypeface = Typeface.Default.GlyphTypeface;
        _skiaTypeface = (SkiaTypeface)_glyphTypeface.PlatformTypeface;
        _font = _skiaTypeface.CreateSKFont(Size);

        var map = _glyphTypeface.CharacterToGlyphMap;
        var pool = new List<ushort>();
        for (var c = '!'; c <= '~'; c++)
        {
            if (map.ContainsGlyph(c))
            {
                pool.Add(map[c]);
            }
        }

        _glyphPool = pool.ToArray();
    }

    [GlobalSetup]
    public void Setup()
    {
        _glyphIds = new ushort[GlyphCount];

        for (var i = 0; i < GlyphCount; i++)
        {
            _glyphIds[i] = _glyphPool[i % _glyphPool.Length];
        }

        _skBounds = new SKRect[GlyphCount];
        _metrics = new GlyphMetrics[GlyphCount];
    }

    // ---- Kernel: SKFont already created; measure only the bounds query. ----

    [Benchmark(Baseline = true)]
    public void Skia_BoundsOnly()
    {
        _font.GetGlyphWidths(_glyphIds, null, _skBounds.AsSpan(0, GlyphCount));
    }

    [Benchmark]
    public void Table_BoundsOnly()
    {
        _glyphTypeface.TryGetGlyphMetrics(_glyphIds, _metrics.AsSpan(0, GlyphCount));
    }

    // ---- Realistic per-run: Skia pays SKFont creation; the table path doesn't. ----

    [Benchmark]
    public void Skia_PerRun()
    {
        using var font = _skiaTypeface.CreateSKFont(Size);

        var bounds = ArrayPool<SKRect>.Shared.Rent(GlyphCount);
        font.GetGlyphWidths(_glyphIds, null, bounds.AsSpan(0, GlyphCount));
        ArrayPool<SKRect>.Shared.Return(bounds);
    }

    [Benchmark]
    public void Table_PerRun()
    {
        Span<GlyphMetrics> metrics = GlyphCount <= 256
            ? stackalloc GlyphMetrics[GlyphCount]
            : new GlyphMetrics[GlyphCount];

        _glyphTypeface.TryGetGlyphMetrics(_glyphIds, metrics);
    }

    public void Dispose()
    {
        _font.Dispose();
        _app?.Dispose();
    }
}
