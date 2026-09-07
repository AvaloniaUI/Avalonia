using System;
using Avalonia.Logging;
using Avalonia.Media.Fonts;
using Avalonia.Media.TextFormatting.Unicode;
using static Avalonia.Media.TextFormatting.FormattingObjectPool;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// A text run that holds text characters.
    /// </summary>
    public class TextCharacters : TextRun
    {
        // NUL characters render nothing but must keep their position in the run. WORD JOINER (U+2060)
        // is a zero-width, default-ignorable, non-breaking filler; unlike ZERO WIDTH SPACE (U+200B)
        // it introduces no line-break opportunity, matching NUL's lack of break semantics.
        private const char WordJoiner = '\u2060';
        private static readonly string s_wordJoinerRun = new string(WordJoiner, 8);

        /// <summary>
        /// Constructs a run for text content from a string.
        /// </summary>
        public TextCharacters(string text, TextRunProperties textRunProperties)
            : this(text.AsMemory(), textRunProperties)
        {
        }

        /// <summary>
        /// Constructs a run for text content from a memory region.
        /// </summary>
        public TextCharacters(ReadOnlyMemory<char> text, TextRunProperties textRunProperties)
        {
            if (textRunProperties.FontRenderingEmSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(textRunProperties), textRunProperties.FontRenderingEmSize,
                    $"Invalid {nameof(TextRunProperties.FontRenderingEmSize)}");
            }

            Text = text;
            Properties = textRunProperties;
        }

        /// <inheritdoc />
        public override int Length
            => Text.Length;

        /// <inheritdoc />
        public override ReadOnlyMemory<char> Text { get; }

        /// <inheritdoc />
        public override TextRunProperties Properties { get; }

        /// <summary>
        /// Gets a list of <see cref="UnshapedTextRun"/>.
        /// </summary>
        /// <returns>The shapeable text characters.</returns>
        internal void GetShapeableCharacters(ReadOnlyMemory<char> text, sbyte biDiLevel,
            FontManager fontManager, ref TextRunProperties? previousProperties, RentedList<TextRun> results)
        {
            var properties = Properties;

            while (!text.IsEmpty)
            {
                var shapeableRun = CreateShapeableRun(text, properties, biDiLevel, fontManager, ref previousProperties);

                results.Add(shapeableRun);

                text = text.Slice(shapeableRun.Length);

                // Whitespace says nothing about which font the text around it wants, and it belongs to
                // the default typeface whenever that covers it - so a run of pure whitespace must not
                // become the anti-thrashing bias for what follows. Otherwise the words on either side
                // of a space each resolve their fallback from scratch and can land on different fonts.
                if (!IsWhiteSpaceOnly(shapeableRun.Text.Span))
                {
                    previousProperties = shapeableRun.Properties;
                }
            }
        }

        /// <summary>
        /// Returns whether every codepoint in <paramref name="text"/> is whitespace. Returns on the
        /// first codepoint that isn't, so a run of text costs a single lookup.
        /// </summary>
        private static bool IsWhiteSpaceOnly(ReadOnlySpan<char> text)
        {
            var codepoints = new CodepointEnumerator(text);

            while (codepoints.MoveNext(out var codepoint))
            {
                if (!codepoint.IsWhiteSpace)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Creates a shapeable text run with unique properties.
        /// </summary>
        /// <param name="text">The characters to create text runs from.</param>
        /// <param name="defaultProperties">The default text run properties.</param>
        /// <param name="biDiLevel">The bidi level of the run.</param>
        /// <param name="fontManager">The font manager to use.</param>
        /// <param name="previousProperties"></param>
        /// <returns>A list of shapeable text runs.</returns>
        private static UnshapedTextRun CreateShapeableRun(ReadOnlyMemory<char> text,
            TextRunProperties defaultProperties, sbyte biDiLevel, FontManager fontManager,
            ref TextRunProperties? previousProperties)
        {
            var defaultTypeface = defaultProperties.Typeface;
            var defaultGlyphTypeface = defaultProperties.CachedGlyphTypeface;
            var previousTypeface = previousProperties?.Typeface;
            var previousGlyphTypeface = previousProperties?.CachedGlyphTypeface;
            var textSpan = text.Span;

            var count = 0;
            var codepoints = new CodepointEnumerator(textSpan);

            while(codepoints.MoveNext(out var firstCodepoint) && firstCodepoint.Value == 0)
            {
                count++;
            }

            //Detect null terminator
            if (count > 0)
            {
                // Reuse a cached run of WORD JOINERs for the common short case to avoid an allocation.
                var nullReplacement = count <= s_wordJoinerRun.Length
                    ? s_wordJoinerRun.AsMemory(0, count)
                    : new string(WordJoiner, count).AsMemory();

                return new UnshapedTextRun(nullReplacement, defaultProperties, biDiLevel);
            }

            // The first scalar's script drives both the locale-sensitivity check and the complex-script
            // capability preference below.
            var firstScript = Codepoint.ReadAt(textSpan, 0, out _).Script;

            // The previous run's font is reused as a cheap anti-thrashing bias, but it bypasses the
            // culture-aware fallback scorer. For locale-sensitive scripts (CJK Han unification) skip
            // the reuse when the culture changed between runs so the culture-scored fallback runs
            // instead - otherwise e.g. a zh run's font would pin ja text. Same-culture runs keep the
            // reuse, so the common case pays only a culture comparison.
            var allowPreviousTypeface = true;

            if (previousGlyphTypeface is not null &&
                !Equals(previousProperties!.CultureInfo, defaultProperties.CultureInfo) &&
                FontFallbackScriptHints.IsLocaleSensitive(firstScript))
            {
                allowPreviousTypeface = false;
            }

            // Capability preference (Strategy A): for a complex script, first try fonts that declare
            // the script in GSUB/GPOS (so they can actually shape it), then fall back to cmap-only
            // coverage. Simple scripts use a single cmap tier, so the common path is unchanged.
            var capabilityTiers =
                FontFallbackScriptHints.TryGetComplexShapingTags(firstScript, out _, out _) ? 2 : 1;

            for (var capabilityTier = 0; capabilityTier < capabilityTiers; capabilityTier++)
            {
                // Tier 0 (only present when there are two tiers) requires the font to declare shaping
                // support for the script; tier 1 accepts cmap coverage alone (the historical gate).
                var requireShapingCapability = capabilityTiers == 2 && capabilityTier == 0;

                // When this tier requires shaping capability, constrain the fallback search to fonts
                // that can shape the script (Script.Unknown = the historical, unconstrained search).
                var shapingConstraint = requireShapingCapability ? firstScript : Script.Unknown;

                // Coverage tiers (full cluster, then base-only) run inside each capability tier. The
                // fallback is resolved once per capability tier because the search constraint differs.
                Typeface fallbackTypeface = default;
                GlyphTypeface? fallbackGlyphTypeface = null;
                var fallbackResolved = false;

                // A primary that cannot shape this tier's script is not a valid "return target" for
                // text: handing clusters back to it would block a shaping-capable fallback that merely
                // shares the primary's cmap. It still reclaims the spacing whitespace between the
                // words, which needs no shaping - see TryGetShapeableLength.
                var defaultCanShape = !requireShapingCapability || defaultGlyphTypeface.CanShapeScript(firstScript);

                for (var pass = 0; pass < 2; pass++)
                {
                    var requireFullCluster = pass == 0;

                    if (defaultCanShape &&
                        TryGetShapeableLength(textSpan, defaultGlyphTypeface, null, defaultCanShapeScript: false,
                            requireFullCluster, out count))
                    {
                        // Primary font: the properties already carry this typeface, so reuse them
                        // directly. This avoids a needless copy and preserves a custom
                        // TextRunProperties subclass that WithTypeface would otherwise flatten.
                        return new UnshapedTextRun(text.Slice(0, count), defaultProperties, biDiLevel);
                    }

                    if (allowPreviousTypeface && previousGlyphTypeface is not null &&
                        (!requireShapingCapability || previousGlyphTypeface.CanShapeScript(firstScript)) &&
                        TryGetShapeableLength(textSpan, previousGlyphTypeface, defaultGlyphTypeface, defaultCanShape,
                            requireFullCluster, out count))
                    {
                        return new UnshapedTextRun(text.Slice(0, count),
                            defaultProperties.WithTypeface(previousTypeface!.Value), biDiLevel);
                    }

                    // Resolve the fallback once, after the primary/previous probes fail. It is keyed on
                    // the first scalar the primary font cannot render - the base for an unsupported
                    // script, or the combining mark for an otherwise-supported cluster - so the search
                    // can find a font for the mark, not just the base. Reused by every pass.
                    if (!fallbackResolved)
                    {
                        fallbackResolved = true;

                        var fallbackCodepoint = GetFallbackCodepoint(textSpan, defaultGlyphTypeface);

                        if (fontManager.TryMatchCharacter(fallbackCodepoint, defaultTypeface.Style, defaultTypeface.Weight,
                                defaultTypeface.Stretch, defaultTypeface.FontFamily, defaultProperties.CultureInfo,
                                shapingConstraint, out fallbackTypeface)
                            && !fontManager.TryGetGlyphTypeface(fallbackTypeface, out fallbackGlyphTypeface))
                        {
                            // The platform matched a fallback family but its glyph typeface could not
                            // be loaded; the cluster degrades to .notdef. Surface it for diagnosis.
                            Logger.TryGet(LogEventLevel.Warning, LogArea.Fonts)?.Log(null,
                                "Matched fallback typeface {FamilyName} for codepoint U+{Codepoint} but could not load its glyph typeface.",
                                fallbackTypeface.FontFamily.Name, ((uint)fallbackCodepoint).ToString("X4"));
                        }
                    }

                    if (fallbackGlyphTypeface is not null &&
                        TryGetShapeableLength(textSpan, fallbackGlyphTypeface, defaultGlyphTypeface, defaultCanShape,
                            requireFullCluster, out count))
                    {
                        return new UnshapedTextRun(text.Slice(0, count),
                            defaultProperties.WithTypeface(fallbackTypeface), biDiLevel);
                    }
                }
            }

            // No font (not even a last-resort match) covers the first cluster. Coalesce the
            // following clusters that likewise have no home into a single .notdef ("tofu") run,
            // then hand control back so the next run can be selected normally. We must stop as
            // soon as a cluster the primary font - or any fallback - can render is reached;
            // otherwise a renderable cluster following an unmatchable one would be swallowed as
            // tofu too (e.g. a private-use codepoint immediately followed by CJK text).
            var enumerator = new GraphemeEnumerator(textSpan);

            while (enumerator.MoveNext(out var grapheme))
            {
                var firstCodepoint = grapheme.FirstCodepoint;

                if (!firstCodepoint.IsWhiteSpace)
                {
                    // Primary font regained coverage - return to it.
                    if (defaultGlyphTypeface.CharacterToGlyphMap.TryGetGlyph(firstCodepoint, out _))
                    {
                        break;
                    }

                    // A fallback exists for this cluster - stop so the next run can use it. The
                    // first cluster is skipped (count == 0): we already know it has no match.
                    if (count > 0 &&
                        fontManager.TryMatchCharacter(firstCodepoint, defaultTypeface.Style, defaultTypeface.Weight,
                            defaultTypeface.Stretch, defaultTypeface.FontFamily, defaultProperties.CultureInfo, out _))
                    {
                        break;
                    }
                }

                count += grapheme.Length;
            }

            return new UnshapedTextRun(text.Slice(0, count), defaultProperties, biDiLevel);
        }

        /// <summary>
        /// Tries to get a shapeable length that is supported by the specified typeface.
        /// </summary>
        /// <param name="text">The characters to shape.</param>
        /// <param name="glyphTypeface">The typeface that is used to find matching characters.</param>
        /// <param name="defaultGlyphTypeface">The default typeface, or <c>null</c> when there is none to
        /// return to (the probe for the default typeface itself).</param>
        /// <param name="defaultCanShapeScript">
        /// Whether the default typeface can shape this run's script. When <c>false</c> it only reclaims
        /// spacing whitespace, which needs no shaping.
        /// </param>
        /// <param name="requireFullCluster">
        /// When <c>true</c>, a grapheme cluster only counts as supported when the typeface has a glyph
        /// for every scalar it contains (base plus combining marks); when <c>false</c>, only the base
        /// scalar is tested.
        /// </param>
        /// <param name="length">The shapeable length.</param>
        /// <returns></returns>
        internal static bool TryGetShapeableLength(
            ReadOnlySpan<char> text,
            GlyphTypeface glyphTypeface,
            GlyphTypeface? defaultGlyphTypeface,
            bool defaultCanShapeScript,
            bool requireFullCluster,
            out int length)
        {
            length = 0;
            var script = Script.Unknown;

            if (text.IsEmpty)
            {
                return false;
            }

            var enumerator = new GraphemeEnumerator(text);

            while (enumerator.MoveNext(out var currentGrapheme))
            {
                var currentCodepoint = currentGrapheme.FirstCodepoint;
                var currentScript = currentCodepoint.Script;

                if(currentCodepoint.Value == 0)
                {
                    //Do not include null terminators
                    break;
                }

                var clusterText = text.Slice(currentGrapheme.Offset, currentGrapheme.Length);

                // A fallback run ends where the default typeface regains coverage, spacing whitespace
                // included - practically every font maps U+0020, so exempting it would let the run
                // shape the following space with the fallback's own advance. A default typeface that
                // cannot shape this script still reclaims that whitespace, which carries no shaping.
                // Only Zs qualifies: control and format codepoints (bidi controls, prepended number
                // signs) keep their cluster with the probed font.
                if (defaultGlyphTypeface != null
                    && (defaultCanShapeScript || currentCodepoint.GeneralCategory == GeneralCategory.SpaceSeparator)
                    && ClusterIsCovered(clusterText, currentCodepoint, defaultGlyphTypeface, requireFullCluster))
                {
                    break;
                }

                //Stop at the first cluster this typeface can't render. A cluster that only holds a
                //default ignorable (a stray variation selector, say) renders nothing whichever font
                //it lands on, so it never ends the run.
                if (NeedsGlyph(currentCodepoint) &&
                    !ClusterIsCovered(clusterText, currentCodepoint, glyphTypeface, requireFullCluster))
                {
                    break;
                }

                if (currentScript != script)
                {
                    if (script is Script.Unknown || currentScript != Script.Common &&
                        script is Script.Common or Script.Inherited)
                    {
                        script = currentScript;
                    }
                    else
                    {
                        if (currentScript != Script.Inherited && currentScript != Script.Common)
                        {
                            break;
                        }
                    }
                }

                length += currentGrapheme.Length;
            }

            return length > 0;
        }

        /// <summary>
        /// Determines whether the codepoint is one a font is expected to provide a glyph for. Break
        /// chars, control codepoints and default ignorables (variation selectors, joiners, ...) never
        /// render - demanding a glyph for those would reject fonts that cover the cluster's actual
        /// content. Format codepoints are not excluded wholesale: the invisible ones are all default
        /// ignorable, while the rest (prepended concatenation marks like U+0600, interlinear
        /// annotation chars) are visible and do need a glyph.
        /// </summary>
        private static bool NeedsGlyph(Codepoint codepoint)
            => !codepoint.IsBreakChar
               && codepoint.GeneralCategory != GeneralCategory.Control
               && !codepoint.IsDefaultIgnorable;

        /// <summary>
        /// Determines whether <paramref name="glyphTypeface"/> can render the first grapheme cluster in
        /// <paramref name="clusterText"/>. For a single-scalar cluster, or when
        /// <paramref name="requireFullCluster"/> is <c>false</c>, only the base scalar is tested.
        /// Otherwise every scalar that needs a glyph must be present, so a base+mark cluster is only
        /// covered by a font that has the marks too.
        /// </summary>
        private static bool ClusterIsCovered(ReadOnlySpan<char> clusterText, Codepoint firstCodepoint,
            GlyphTypeface glyphTypeface, bool requireFullCluster)
        {
            var baseLength = firstCodepoint.Value > 0xFFFF ? 2 : 1;

            if (!requireFullCluster || clusterText.Length <= baseLength)
            {
                return glyphTypeface.CharacterToGlyphMap.TryGetGlyph(firstCodepoint, out _);
            }

            var codepoints = new CodepointEnumerator(clusterText);

            while (codepoints.MoveNext(out var codepoint))
            {
                if (!NeedsGlyph(codepoint))
                {
                    continue;
                }

                if (!glyphTypeface.CharacterToGlyphMap.TryGetGlyph(codepoint, out _))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns the first scalar of the run's first grapheme cluster that
        /// <paramref name="defaultGlyphTypeface"/> cannot render - the base for an unsupported script,
        /// or a combining mark for an otherwise-supported cluster. Keying the fallback search on this
        /// lets it find a font for the mark, not just the base. Falls back to the cluster's first
        /// scalar when every scalar is already covered.
        /// </summary>
        private static Codepoint GetFallbackCodepoint(ReadOnlySpan<char> text, GlyphTypeface defaultGlyphTypeface)
        {
            var graphemeEnumerator = new GraphemeEnumerator(text);

            if (!graphemeEnumerator.MoveNext(out var grapheme))
            {
                return Codepoint.ReplacementCodepoint;
            }

            var codepoints = new CodepointEnumerator(text.Slice(grapheme.Offset, grapheme.Length));

            while (codepoints.MoveNext(out var codepoint))
            {
                if (!NeedsGlyph(codepoint))
                {
                    continue;
                }

                if (!defaultGlyphTypeface.CharacterToGlyphMap.TryGetGlyph(codepoint, out _))
                {
                    return codepoint;
                }
            }

            return grapheme.FirstCodepoint;
        }
    }
}
