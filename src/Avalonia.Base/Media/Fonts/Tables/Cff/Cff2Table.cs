using System;
using System.Buffers.Binary;
using Avalonia.Logging;
using Avalonia.Media.Fonts.Tables.Variation;
using Avalonia.Platform;

namespace Avalonia.Media.Fonts.Tables.Cff
{
    /// <summary>
    /// Reader for the <c>CFF2</c> table — the variation-aware evolution of CFF. Unlike CFF it has no
    /// Name / String / Encoding / charset structures and its Top DICT is a single fixed-length DICT
    /// (not an INDEX); its INDEXes use 32-bit counts. Charstrings are Type 2 without
    /// <c>endchar</c> / width / <c>seac</c>, and add the <c>vsindex</c> / <c>blend</c> operators that
    /// interpolate operands through the table's <c>vstore</c> (an <see cref="ItemVariationStore"/>)
    /// at the typeface's active variation coordinates.
    /// </summary>
    /// <remarks>
    /// Every CFF2 font is FD-organised (FDArray + optional FDSelect), like CID CFF; an absent FDSelect
    /// means all glyphs use Font DICT 0. Non-default Top-DICT <c>FontMatrix</c> scaling is out of scope.
    /// </remarks>
    internal sealed class Cff2Table
    {
        internal const string TableName = "CFF2";

        internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse(TableName);

        private const int OpCharStrings = 17;
        private const int OpVStore = 24;
        private const int OpFdArray = CffDict.TwoByteOperatorBase + 36;
        private const int OpFdSelect = CffDict.TwoByteOperatorBase + 37;

        // CFF2 raises the Type 2 operand-stack depth limit from 48 to 513 to accommodate blends.
        private const int StackSize = 513;

        private readonly CffIndex _charStrings;
        private readonly CffIndex _globalSubrs;
        private readonly CffIndex[] _fdLocalSubrs;
        private readonly FdSelect? _fdSelect;
        private readonly ItemVariationStore? _vStore;

        private Cff2Table(CffIndex charStrings, CffIndex globalSubrs, CffIndex[] fdLocalSubrs,
            FdSelect? fdSelect, ItemVariationStore? vStore)
        {
            _charStrings = charStrings;
            _globalSubrs = globalSubrs;
            _fdLocalSubrs = fdLocalSubrs;
            _fdSelect = fdSelect;
            _vStore = vStore;
        }

        /// <summary>Number of glyph charstrings in the font.</summary>
        public int GlyphCount => _charStrings.Count;

        /// <summary>
        /// Loads the CFF2 table. <paramref name="axisCount"/> is the font's fvar axis count, used to
        /// validate the <c>vstore</c>'s <see cref="ItemVariationStore"/>.
        /// </summary>
        public static bool TryLoad(GlyphTypeface glyphTypeface, int axisCount, out Cff2Table? cff2Table)
        {
            cff2Table = null;

            if (!glyphTypeface.PlatformTypeface.TryGetTable(Tag, out var data) || data.Length < 5)
            {
                return false;
            }

            try
            {
                var span = data.Span;
                int headerSize = span[2];
                int topDictLength = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(3, 2));

                if (headerSize + topDictLength > data.Length)
                {
                    return false;
                }

                // The Top DICT is a single fixed-length DICT; the Global Subr INDEX follows it.
                var topDict = CffDict.Parse(span.Slice(headerSize, topDictLength));
                var globalSubrs = CffIndex.Read(data, headerSize + topDictLength, wideCount: true);

                int charStringsOffset = topDict.GetInt(OpCharStrings, 0);
                if (charStringsOffset <= 0 || charStringsOffset >= data.Length)
                {
                    return false;
                }

                var charStrings = CffIndex.Read(data, charStringsOffset, wideCount: true);

                int fdArrayOffset = topDict.GetInt(OpFdArray, 0);
                if (fdArrayOffset <= 0 || fdArrayOffset >= data.Length)
                {
                    return false;
                }

                var fdArray = CffIndex.Read(data, fdArrayOffset, wideCount: true);
                var fdLocalSubrs = new CffIndex[fdArray.Count];
                for (int i = 0; i < fdArray.Count; i++)
                {
                    fdLocalSubrs[i] = CffTable.ParseLocalSubrs(data, CffDict.Parse(fdArray[i].Span), wideCount: true);
                }

                // FDSelect is optional — absent means every glyph uses Font DICT 0.
                FdSelect? fdSelect = null;
                int fdSelectOffset = topDict.GetInt(OpFdSelect, 0);
                if (fdSelectOffset > 0 && fdSelectOffset < data.Length)
                {
                    fdSelect = FdSelect.Parse(data, fdSelectOffset, charStrings.Count);
                }

                // The vstore is an ItemVariationStore preceded by a uint16 length.
                ItemVariationStore? vStore = null;
                int vStoreOffset = topDict.GetInt(OpVStore, 0);
                if (vStoreOffset > 0 && vStoreOffset + 2 <= data.Length)
                {
                    int storeLength = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(vStoreOffset, 2));
                    int storeStart = vStoreOffset + 2;

                    if (storeStart + storeLength <= data.Length)
                    {
                        ItemVariationStore.TryLoad(data.Slice(storeStart, storeLength), axisCount, out vStore);
                    }
                }

                cff2Table = new Cff2Table(charStrings, globalSubrs, fdLocalSubrs, fdSelect, vStore);
                return true;
            }
            catch (Exception ex)
            {
                if (Logger.TryGet(LogEventLevel.Warning, LogArea.Visual, out var log))
                {
                    log.Log(null, "Failed to load CFF2 table: {Message}", ex.Message);
                }

                cff2Table = null;
                return false;
            }
        }

        /// <summary>
        /// Builds the outline for <paramref name="glyphIndex"/> into <paramref name="context"/>, with
        /// <paramref name="transform"/> applied and the charstring's blends evaluated at
        /// <paramref name="activeCoords"/>. Returns <c>false</c> for an out-of-range glyph or a
        /// malformed charstring.
        /// </summary>
        public bool TryBuildGlyphGeometry(int glyphIndex, Matrix transform, IGeometryContext context,
            ReadOnlySpan<float> activeCoords)
        {
            if ((uint)glyphIndex >= (uint)_charStrings.Count)
            {
                return false;
            }

            int fd = _fdSelect?.GetFd(glyphIndex) ?? 0;
            if ((uint)fd >= (uint)_fdLocalSubrs.Length)
            {
                return false;
            }

            context.SetFillRule(FillRule.NonZero);

            try
            {
                Span<double> stack = stackalloc double[StackSize];

                // Sized to the global region count (an upper bound on any vsindex's region count);
                // empty when the font has no vstore, in which case blend is inert.
                int regionCount = _vStore?.RegionCount ?? 0;
                Span<float> blendScalers = stackalloc float[regionCount];

                var interpreter = new Type2CharStringInterpreter(
                    context, transform, _globalSubrs, _fdLocalSubrs[fd], stack, _vStore, activeCoords, blendScalers);
                interpreter.Run(_charStrings[glyphIndex]);
                return true;
            }
            catch (Exception ex)
            {
                if (Logger.TryGet(LogEventLevel.Warning, LogArea.Visual, out var log))
                {
                    log.Log(null, "Failed to build CFF2 glyph {GlyphIndex}: {Message}", glyphIndex, ex.Message);
                }

                return false;
            }
        }
    }
}
