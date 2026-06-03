using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;
using Avalonia.Skia;
using SkiaSharp;

namespace Avalonia.Benchmarks.Media.Variation
{
    /// <summary>
    /// Shared setup helpers for the Variation benchmarks. Owns the typeface load logic
    /// and the canonical glyph-ID arrays used as inputs to the batch advance / metrics
    /// benchmarks. Individual benchmark classes pull what they need from here so they
    /// stay focused on the API call under measurement.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Typefaces are loaded directly via the asset-resource stream + <see cref="SkiaTypeface"/>
    /// constructor (the same pattern <c>tests/Avalonia.RenderTests/Media/GlyphOutlineRenderTests</c>
    /// uses) so we can address Inter-Regular and InterVariable independently, sidestepping
    /// the family-name collision they'd cause if resolved through <see cref="FontManager"/>.
    /// </para>
    /// <para>
    /// The glyph arrays are derived from short and long Latin strings via the typeface's
    /// CharacterToGlyphMap, mirroring the distribution a real text layout would hit. For
    /// the 2000-glyph batch we cycle a 200-char paragraph 10× because that better matches
    /// real long-paragraph layout (lots of repeated common glyphs) than picking 2000
    /// distinct rare ones.
    /// </para>
    /// </remarks>
    internal static class VariationFixtures
    {
        public const string InterRegularAsset =
            "resm:Avalonia.Benchmarks.Assets.Inter-Regular.ttf?assembly=Avalonia.Benchmarks";

        public const string InterVariableAsset =
            "resm:Avalonia.Benchmarks.Assets.InterVariable.ttf?assembly=Avalonia.Benchmarks";

        public static readonly OpenTypeTag WghtTag = OpenTypeTag.Parse("wght");

        // A 20-glyph "word-size" sample. Short enough that per-batch dispatch overhead
        // is visible. Roughly the length of a typical English word boundary.
        private const string WordSample = "Hello there, world!!";

        // A ~200-char "line-size" sample. Representative of a single typeset line.
        private const string LineSample =
            "The quick brown fox jumps over the lazy dog. Pack my box with five dozen liquor jugs. " +
            "How vexingly quick daft zebras jump! The five boxing wizards jump quickly. " +
            "Sphinx of black quartz, judge my vow.";

        /// <summary>
        /// Loads Inter-Regular (the static-font baseline for regression comparison).
        /// </summary>
        public static GlyphTypeface LoadInterRegular() => LoadTypeface(InterRegularAsset);

        /// <summary>
        /// Loads Inter Variable at its default instance. <see cref="GlyphTypeface.WithVariation"/>
        /// returns this same instance unless a non-default settings value is requested.
        /// </summary>
        public static GlyphTypeface LoadInterVariable() => LoadTypeface(InterVariableAsset);

        /// <summary>
        /// Builds a <see cref="FontVariationSettings"/> for Inter Variable at the
        /// requested weight (user-space, 100..900).
        /// </summary>
        public static FontVariationSettings WghtSettings(GlyphTypeface gt, float weight)
            => gt.CreateVariationSettings(new Dictionary<OpenTypeTag, float> { [WghtTag] = weight });

        /// <summary>
        /// Returns a 20-glyph word-sized array. Uses the typeface's cmap to translate
        /// from the canonical word sample — caller-provided so static and variable
        /// fonts produce the same glyph indices when their cmaps agree (Inter does).
        /// </summary>
        public static ushort[] BuildWordGlyphs(GlyphTypeface gt) => MapChars(gt, WordSample, count: 20);

        /// <summary>
        /// Returns a ~200-glyph line-sized array.
        /// </summary>
        public static ushort[] BuildLineGlyphs(GlyphTypeface gt) => MapChars(gt, LineSample, count: 200);

        /// <summary>
        /// Returns a 2000-glyph "long paragraph" array. The same 200-glyph line cycled
        /// 10× — closer to the glyph-frequency distribution a real paragraph hits than
        /// 2000 distinct random characters would be.
        /// </summary>
        public static ushort[] BuildParagraphGlyphs(GlyphTypeface gt)
        {
            var line = BuildLineGlyphs(gt);
            var result = new ushort[2000];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = line[i % line.Length];
            }
            return result;
        }

        private static GlyphTypeface LoadTypeface(string assetUri)
        {
            var assetLoader = new StandardAssetLoader();
            using var stream = assetLoader.Open(new Uri(assetUri));
            using var memory = new MemoryStream();
            stream.CopyTo(memory);

            // SKData.CreateCopy keeps the font bytes alive for the SKTypeface's table
            // reads even after the source stream disposes — the same pattern the
            // GlyphOutlineRenderTests use to avoid the family-name collision through
            // FontManager.
            var skData = SKData.CreateCopy(memory.ToArray());
            var skTypeface = SKTypeface.FromData(skData)
                ?? throw new InvalidOperationException(
                    $"SkiaSharp failed to load the font at '{assetUri}'.");

            return new GlyphTypeface(new SkiaTypeface(skTypeface, FontSimulations.None));
        }

        private static ushort[] MapChars(GlyphTypeface gt, string sample, int count)
        {
            var cmap = gt.CharacterToGlyphMap;
            var result = new ushort[count];

            // Walk the sample, skipping any chars the font doesn't have a glyph for.
            // Inter covers every codepoint in our samples, so we never hit the skip
            // path in practice — it's defensive against future font swaps.
            var written = 0;
            var index = 0;
            while (written < count)
            {
                var ch = sample[index % sample.Length];
                if (cmap.TryGetGlyph(ch, out var glyphId))
                {
                    result[written++] = glyphId;
                }
                index++;
                if (index > sample.Length * 4)
                {
                    throw new InvalidOperationException(
                        $"Font has no glyph coverage for the sample '{sample}'.");
                }
            }

            return result;
        }
    }
}
