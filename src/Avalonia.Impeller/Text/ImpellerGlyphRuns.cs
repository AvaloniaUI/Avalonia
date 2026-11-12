using System.Runtime.CompilerServices;
using Avalonia.Skia;
using NImpeller;
using SkiaSharp;

namespace Avalonia.Impeller;

/// <summary>Glyph runs drawn as filled outlines, relative to the baseline origin.</summary>
internal static class ImpellerGlyphRuns
{
    private static readonly ConditionalWeakTable<GlyphRunImpl, ImpellerPath> s_outlines = new();

    public static ImpellerPath? GetOutline(GlyphRunImpl run)
    {
        if (s_outlines.TryGetValue(run, out var cached))
            return cached;
        var outline = CreateOutline(run);
        return outline is null ? null : s_outlines.GetValue(run, _ => outline);
    }

    private static ImpellerPath? CreateOutline(GlyphRunImpl run)
    {
        using var font = run.Typeface.CreateSKFont((float)run.FontRenderingEmSize);
        font.Hinting = SKFontHinting.None;

        using var path = new SKPath();
        var glyphs = run.GlyphIndices;
        var positions = run.GlyphPositions;
        for (var i = 0; i < glyphs.Length; i++)
        {
            using var glyph = font.GetGlyphPath(glyphs[i]);
            if (glyph is { IsEmpty: false })
                path.AddPath(glyph, positions[i].X, positions[i].Y);
        }

        return ImpellerPaths.Convert(path);
    }
}
