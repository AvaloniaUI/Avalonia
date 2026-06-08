using System;
using Avalonia.Logging;
using Avalonia.Platform;

namespace Avalonia.Media.Fonts.Tables.Cff
{
    /// <summary>
    /// Reader for the <c>CFF </c> table — PostScript / Type 2 glyph outlines, the <c>.otf</c> flavour
    /// and the counterpart to <see cref="Glyf.GlyfTable"/>'s TrueType <c>glyf</c> outlines. Parses the
    /// header, the Name / Top DICT / String / Global Subr / CharStrings INDEXes and the Private DICT,
    /// then interprets a glyph's Type 2 charstring on demand via <see cref="Type2CharStringInterpreter"/>.
    /// </summary>
    /// <remarks>
    /// CID-keyed CFF (FDArray / FDSelect) is detected but not yet rendered (a later phase) — such a
    /// glyph returns <c>false</c>, so <c>GetGlyphOutline</c> yields <c>null</c> exactly as it did
    /// before CFF support. Non-default Top-DICT <c>FontMatrix</c> scaling is likewise out of scope.
    /// </remarks>
    internal sealed class CffTable
    {
        internal const string TableName = "CFF ";

        internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse(TableName);

        // Top DICT / Private DICT operator keys (see CffDict for the 12xx two-byte encoding).
        private const int OpCharStrings = 17;
        private const int OpPrivate = 18;
        private const int OpLocalSubrs = 19;
        private const int OpRos = CffDict.TwoByteOperatorBase + 30;

        private readonly CffIndex _charStrings;
        private readonly CffIndex _globalSubrs;
        private readonly CffIndex _localSubrs;
        private readonly bool _isCid;

        private CffTable(CffIndex charStrings, CffIndex globalSubrs, CffIndex localSubrs, bool isCid)
        {
            _charStrings = charStrings;
            _globalSubrs = globalSubrs;
            _localSubrs = localSubrs;
            _isCid = isCid;
        }

        /// <summary>Number of glyph charstrings in the font.</summary>
        public int GlyphCount => _charStrings.Count;

        public static bool TryLoad(GlyphTypeface glyphTypeface, out CffTable? cffTable)
        {
            cffTable = null;

            if (!glyphTypeface.PlatformTypeface.TryGetTable(Tag, out var data) || data.Length < 4)
            {
                return false;
            }

            try
            {
                int headerSize = data.Span[2];

                // Header → Name INDEX → Top DICT INDEX → String INDEX → Global Subr INDEX.
                var nameIndex = CffIndex.Read(data, headerSize);
                var topDictIndex = CffIndex.Read(data, nameIndex.EndOffset);
                var stringIndex = CffIndex.Read(data, topDictIndex.EndOffset);
                var globalSubrs = CffIndex.Read(data, stringIndex.EndOffset);

                if (topDictIndex.Count < 1)
                {
                    return false;
                }

                var topDict = CffDict.Parse(topDictIndex[0].Span);

                int charStringsOffset = topDict.GetInt(OpCharStrings, 0);
                if (charStringsOffset <= 0 || charStringsOffset >= data.Length)
                {
                    return false;
                }

                var charStrings = CffIndex.Read(data, charStringsOffset);

                bool isCid = topDict.Contains(OpRos);

                CffIndex localSubrs = default;
                if (!isCid && topDict.TryGetOperands(OpPrivate, out var priv) && priv.Length == 2)
                {
                    int privateSize = (int)priv[0];
                    int privateOffset = (int)priv[1];

                    if (privateSize > 0 && privateOffset > 0 && privateOffset + privateSize <= data.Length)
                    {
                        var privateDict = CffDict.Parse(data.Span.Slice(privateOffset, privateSize));
                        int localSubrsOffset = privateDict.GetInt(OpLocalSubrs, 0);

                        if (localSubrsOffset > 0)
                        {
                            // The Local Subr offset is relative to the start of the Private DICT.
                            localSubrs = CffIndex.Read(data, privateOffset + localSubrsOffset);
                        }
                    }
                }

                cffTable = new CffTable(charStrings, globalSubrs, localSubrs, isCid);
                return true;
            }
            catch (Exception ex)
            {
                if (Logger.TryGet(LogEventLevel.Warning, LogArea.Visual, out var log))
                {
                    log.Log(null, "Failed to load CFF table: {Message}", ex.Message);
                }

                cffTable = null;
                return false;
            }
        }

        /// <summary>
        /// Builds the outline for <paramref name="glyphIndex"/> into <paramref name="context"/>, with
        /// <paramref name="transform"/> applied. Returns <c>false</c> (no geometry) for out-of-range
        /// glyphs, CID-keyed fonts (not yet supported), or a malformed charstring.
        /// </summary>
        public bool TryBuildGlyphGeometry(int glyphIndex, Matrix transform, IGeometryContext context)
        {
            if (_isCid || (uint)glyphIndex >= (uint)_charStrings.Count)
            {
                return false;
            }

            // Type 2 / PostScript outlines use the non-zero winding rule, same as glyf.
            context.SetFillRule(FillRule.NonZero);

            try
            {
                Span<double> stack = stackalloc double[48];
                var interpreter = new Type2CharStringInterpreter(context, transform, _globalSubrs, _localSubrs, stack);
                interpreter.Run(_charStrings[glyphIndex]);
                return true;
            }
            catch (Exception ex)
            {
                if (Logger.TryGet(LogEventLevel.Warning, LogArea.Visual, out var log))
                {
                    log.Log(null, "Failed to build CFF glyph {GlyphIndex}: {Message}", glyphIndex, ex.Message);
                }

                return false;
            }
        }
    }
}
