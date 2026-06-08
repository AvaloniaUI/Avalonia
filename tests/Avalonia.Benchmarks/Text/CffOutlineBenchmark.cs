using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;
using Avalonia.Skia;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;
using SkiaSharp;

namespace Avalonia.Benchmarks.Text;

/// <summary>
/// Shared font loading for the CFF / CFF2 outline benchmarks. Loads each format
/// directly through <see cref="SkiaTypeface"/> (the same pattern the Variation
/// benchmarks use) so glyf, CFF and CFF2 fonts can be addressed independently.
/// </summary>
internal static class CffFonts
{
    public const string GlyfAsset = "resm:Avalonia.Benchmarks.Assets.Inter-Regular.ttf?assembly=Avalonia.Benchmarks";
    public const string CffAsset = "resm:Avalonia.Benchmarks.Assets.SourceCodePro-Subset.otf?assembly=Avalonia.Benchmarks";
    public const string Cff2Asset = "resm:Avalonia.Benchmarks.Assets.AdobeVFPrototype-Subset.otf?assembly=Avalonia.Benchmarks";

    public static readonly OpenTypeTag WghtTag = OpenTypeTag.Parse("wght");

    public static GlyphTypeface Load(string assetUri)
    {
        var assetLoader = new StandardAssetLoader();
        using var stream = assetLoader.Open(new Uri(assetUri));
        using var memory = new MemoryStream();
        stream.CopyTo(memory);

        var skData = SKData.CreateCopy(memory.ToArray());
        var skTypeface = SKTypeface.FromData(skData)
            ?? throw new InvalidOperationException($"SkiaSharp failed to load the font at '{assetUri}'.");

        return new GlyphTypeface(new SkiaTypeface(skTypeface, FontSimulations.None));
    }

    public static GlyphTypeface Vary(GlyphTypeface gt, float weight)
        => gt.WithVariation(gt.CreateVariationSettings(new Dictionary<OpenTypeTag, float> { [WghtTag] = weight }));

    /// <summary>The font's mapped printable-ASCII glyph ids, used to fill the batch inputs.</summary>
    public static ushort[] BuildPool(GlyphTypeface gt)
    {
        var map = gt.CharacterToGlyphMap;
        var pool = new List<ushort>();

        for (var c = '!'; c <= '~'; c++)
        {
            if (map.ContainsGlyph(c))
            {
                pool.Add(map[c]);
            }
        }

        // The subset fonts cover only a handful of letters; never return an empty pool.
        if (pool.Count == 0)
        {
            pool.Add(1);
        }

        return pool.ToArray();
    }

    public static ushort[] Cycle(ushort[] pool, int count)
    {
        var ids = new ushort[count];

        for (var i = 0; i < count; i++)
        {
            ids[i] = pool[i % pool.Length];
        }

        return ids;
    }
}

/// <summary>
/// Per-glyph control-point bounds across outline formats. The glyf baseline reads the
/// header bbox (O(1) after the loca lookup); CFF / CFF2 store no bbox and interpret the
/// charstring on every call. This benchmark quantifies that asymmetry — and is the gauge
/// for any bounds-caching optimization.
/// </summary>
[MemoryDiagnoser]
public class CffGlyphBoundsBenchmark : IDisposable
{
    private readonly IDisposable _app;

    private readonly GlyphTypeface _glyf;
    private readonly GlyphTypeface _cff;
    private readonly GlyphTypeface _cff2;
    private readonly GlyphTypeface _cff2Varied;

    private readonly ushort[] _glyfPool;
    private readonly ushort[] _cffPool;
    private readonly ushort[] _cff2Pool;

    private ushort[] _glyfIds = Array.Empty<ushort>();
    private ushort[] _cffIds = Array.Empty<ushort>();
    private ushort[] _cff2Ids = Array.Empty<ushort>();
    private GlyphBounds[] _bounds = Array.Empty<GlyphBounds>();

    [Params(1, 16, 256)]
    public int GlyphCount { get; set; }

    public CffGlyphBoundsBenchmark()
    {
        _app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(
            renderInterface: new PlatformRenderInterface(),
            fontManagerImpl: new FontManagerImpl()));

        _glyf = CffFonts.Load(CffFonts.GlyfAsset);
        _cff = CffFonts.Load(CffFonts.CffAsset);
        _cff2 = CffFonts.Load(CffFonts.Cff2Asset);
        _cff2Varied = CffFonts.Vary(_cff2, 900f);

        _glyfPool = CffFonts.BuildPool(_glyf);
        _cffPool = CffFonts.BuildPool(_cff);
        _cff2Pool = CffFonts.BuildPool(_cff2);
    }

    [GlobalSetup]
    public void Setup()
    {
        _glyfIds = CffFonts.Cycle(_glyfPool, GlyphCount);
        _cffIds = CffFonts.Cycle(_cffPool, GlyphCount);
        _cff2Ids = CffFonts.Cycle(_cff2Pool, GlyphCount);
        _bounds = new GlyphBounds[GlyphCount];
    }

    [Benchmark(Baseline = true)]
    public bool Glyf() => _glyf.TryGetGlyphBounds(_glyfIds, _bounds.AsSpan(0, GlyphCount));

    [Benchmark]
    public bool Cff() => _cff.TryGetGlyphBounds(_cffIds, _bounds.AsSpan(0, GlyphCount));

    [Benchmark]
    public bool Cff2() => _cff2.TryGetGlyphBounds(_cff2Ids, _bounds.AsSpan(0, GlyphCount));

    [Benchmark]
    public bool Cff2Varied() => _cff2Varied.TryGetGlyphBounds(_cff2Ids, _bounds.AsSpan(0, GlyphCount));

    public void Dispose() => _app?.Dispose();
}

/// <summary>
/// Single-glyph outline build (<see cref="GlyphTypeface.GetGlyphOutline"/> →
/// <see cref="IGeometryImpl"/>) across outline formats. Measures the geometry-emitting
/// glyf walk / Type 2 charstring interpreter plus the platform geometry construction.
/// </summary>
[MemoryDiagnoser]
public class CffGlyphOutlineBenchmark : IDisposable
{
    private readonly IDisposable _app;

    private readonly GlyphTypeface _glyf;
    private readonly GlyphTypeface _cff;
    private readonly GlyphTypeface _cff2;
    private readonly GlyphTypeface _cff2Varied;

    private readonly ushort _glyfGlyph;
    private readonly ushort _cffGlyph;
    private readonly ushort _cff2Glyph;

    public CffGlyphOutlineBenchmark()
    {
        _app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(
            renderInterface: new PlatformRenderInterface(),
            fontManagerImpl: new FontManagerImpl()));

        _glyf = CffFonts.Load(CffFonts.GlyfAsset);
        _cff = CffFonts.Load(CffFonts.CffAsset);
        _cff2 = CffFonts.Load(CffFonts.Cff2Asset);
        _cff2Varied = CffFonts.Vary(_cff2, 900f);

        // 'O' / 'o' — a curve-heavy glyph present in every test font.
        _glyfGlyph = _glyf.CharacterToGlyphMap['O'];
        _cffGlyph = _cff.CharacterToGlyphMap['O'];
        _cff2Glyph = _cff2.CharacterToGlyphMap['o'];
    }

    [Benchmark(Baseline = true)]
    public IGeometryImpl Glyf() => _glyf.GetGlyphOutline(_glyfGlyph);

    [Benchmark]
    public IGeometryImpl Cff() => _cff.GetGlyphOutline(_cffGlyph);

    [Benchmark]
    public IGeometryImpl Cff2() => _cff2.GetGlyphOutline(_cff2Glyph);

    [Benchmark]
    public IGeometryImpl Cff2Varied() => _cff2Varied.GetGlyphOutline(_cff2Glyph);

    public void Dispose() => _app?.Dispose();
}
