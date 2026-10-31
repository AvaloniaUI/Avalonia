using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
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
    /// CID-keyed CFF is supported: the glyph's Font DICT is resolved through FDSelect and its Local
    /// Subrs come from the matching FDArray entry. Non-default Top-DICT <c>FontMatrix</c> scaling is
    /// out of scope (the common 1000-upem identity case is assumed).
    /// </remarks>
    internal sealed class CffTable
    {
        internal const string TableName = "CFF ";

        internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse(TableName);

        // Top DICT / Private DICT operator keys (see CffDict for the 12xx two-byte encoding).
        private const int OpCharset = 15;
        private const int OpCharStrings = 17;
        private const int OpPrivate = 18;
        private const int OpLocalSubrs = 19;
        private const int OpRos = CffDict.TwoByteOperatorBase + 30;
        private const int OpFdArray = CffDict.TwoByteOperatorBase + 36;
        private const int OpFdSelect = CffDict.TwoByteOperatorBase + 37;

        private readonly ReadOnlyMemory<byte> _table;
        private readonly CffIndex _charStrings;
        private readonly CffIndex _globalSubrs;

        // Top DICT charset: 0 / 1 / 2 select a predefined charset, anything else is the offset of the
        // font's own. Only read to resolve seac accent components, so it is parsed on demand.
        private readonly int _charset;

        // Non-CID fonts use a single Local Subr INDEX (_localSubrs). CID-keyed fonts instead select a
        // Font DICT per glyph via _fdSelect and use that FD's Local Subrs from _fdLocalSubrs.
        private readonly CffIndex _localSubrs;
        private readonly bool _isCid;
        private readonly FdSelect? _fdSelect;
        private readonly CffIndex[]? _fdLocalSubrs;

        private CffTable(ReadOnlyMemory<byte> table, CffIndex charStrings, CffIndex globalSubrs, int charset,
            CffIndex localSubrs, bool isCid, FdSelect? fdSelect, CffIndex[]? fdLocalSubrs)
        {
            _table = table;
            _charset = charset;
            _charStrings = charStrings;
            _globalSubrs = globalSubrs;
            _localSubrs = localSubrs;
            _isCid = isCid;
            _fdSelect = fdSelect;
            _fdLocalSubrs = fdLocalSubrs;
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
                FdSelect? fdSelect = null;
                CffIndex[]? fdLocalSubrs = null;

                if (isCid)
                {
                    // CID-keyed: an FDArray of Font DICTs (each with its own Private DICT / Local Subrs)
                    // plus an FDSelect mapping each glyph to its Font DICT.
                    int fdArrayOffset = topDict.GetInt(OpFdArray, 0);
                    int fdSelectOffset = topDict.GetInt(OpFdSelect, 0);

                    if (fdArrayOffset <= 0 || fdSelectOffset <= 0 ||
                        fdArrayOffset >= data.Length || fdSelectOffset >= data.Length)
                    {
                        return false;
                    }

                    var fdArray = CffIndex.Read(data, fdArrayOffset);
                    fdLocalSubrs = new CffIndex[fdArray.Count];
                    for (int i = 0; i < fdArray.Count; i++)
                    {
                        fdLocalSubrs[i] = ParseLocalSubrs(data, CffDict.Parse(fdArray[i].Span));
                    }

                    fdSelect = FdSelect.Parse(data, fdSelectOffset, charStrings.Count);
                    if (fdSelect is null)
                    {
                        return false;
                    }
                }
                else
                {
                    localSubrs = ParseLocalSubrs(data, topDict);
                }

                int charset = topDict.GetInt(OpCharset, 0);

                cffTable = new CffTable(data, charStrings, globalSubrs, charset, localSubrs, isCid, fdSelect,
                    fdLocalSubrs);
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
        /// Reads the Local Subr INDEX referenced by a DICT's Private entry. Shared by the non-CID Top
        /// DICT, each CID Font DICT, and (via <see cref="Cff2Table"/>) each CFF2 Font DICT. Returns an
        /// empty INDEX when there is no Private / Local Subrs. <paramref name="wideCount"/> selects the
        /// 32-bit (CFF2) vs 16-bit (CFF) INDEX count.
        /// </summary>
        internal static CffIndex ParseLocalSubrs(ReadOnlyMemory<byte> data, CffDict dict, bool wideCount = false)
            => TryParsePrivateDict(data, dict, out var privateDict, out var privateOffset)
                ? ReadLocalSubrs(data, privateDict, privateOffset, wideCount)
                : default;

        /// <summary>
        /// Parses the Private DICT referenced by a DICT's Private entry (size, offset). Returns
        /// <c>false</c> when there is none or it does not fit the table.
        /// </summary>
        internal static bool TryParsePrivateDict(ReadOnlyMemory<byte> data, CffDict dict,
            [NotNullWhen(true)] out CffDict? privateDict, out int privateOffset)
        {
            privateDict = null;
            privateOffset = 0;

            if (!dict.TryGetOperands(OpPrivate, out var priv) || priv.Length != 2)
            {
                return false;
            }

            int privateSize = (int)priv[0];
            privateOffset = (int)priv[1];

            if (privateSize <= 0 || privateOffset <= 0 || privateOffset + privateSize > data.Length)
            {
                return false;
            }

            privateDict = CffDict.Parse(data.Span.Slice(privateOffset, privateSize));
            return true;
        }

        /// <summary>
        /// Reads the Local Subr INDEX of a parsed Private DICT located at <paramref name="privateOffset"/>,
        /// or an empty INDEX when it has none.
        /// </summary>
        internal static CffIndex ReadLocalSubrs(ReadOnlyMemory<byte> data, CffDict privateDict, int privateOffset,
            bool wideCount)
        {
            int localSubrsOffset = privateDict.GetInt(OpLocalSubrs, 0);

            // The Local Subr offset is relative to the start of the Private DICT.
            return localSubrsOffset > 0 ? CffIndex.Read(data, privateOffset + localSubrsOffset, wideCount) : default;
        }

        /// <summary>
        /// Builds the outline for <paramref name="glyphIndex"/> into <paramref name="context"/>, with
        /// <paramref name="transform"/> applied. Returns <c>false</c> (no geometry) for an out-of-range
        /// glyph or a malformed charstring.
        /// </summary>
        public bool TryBuildGlyphGeometry(int glyphIndex, Matrix transform, IGeometryContext context)
        {
            if ((uint)glyphIndex >= (uint)_charStrings.Count)
            {
                return false;
            }

            // CID-keyed fonts select the Local Subrs via the glyph's Font DICT; non-CID fonts use the
            // single shared Local Subr INDEX.
            CffIndex localSubrs;
            if (_isCid)
            {
                if (_fdSelect is null || _fdLocalSubrs is null)
                {
                    return false;
                }

                int fd = _fdSelect.GetFd(glyphIndex);
                if ((uint)fd >= (uint)_fdLocalSubrs.Length)
                {
                    return false;
                }

                localSubrs = _fdLocalSubrs[fd];
            }
            else
            {
                localSubrs = _localSubrs;
            }

            // Type 2 / PostScript outlines use the non-zero winding rule, same as glyf.
            context.SetFillRule(FillRule.NonZero);

            try
            {
                Span<double> stack = stackalloc double[48];

                // seac addresses its components through the Standard Encoding and the charset, which
                // only non-CID fonts have.
                var interpreter = new Type2CharStringInterpreter(context, transform, _globalSubrs, localSubrs, stack,
                    _isCid ? null : this);
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

        /// <summary>
        /// Resolves a seac component, given as a Standard Encoding code, to its glyph's charstring
        /// through the font's charset. Returns <c>false</c> when the code is unencoded, the charset
        /// has no glyph of that name, or the font is CID-keyed.
        /// </summary>
        internal bool TryGetSeacComponent(int standardCode, out ReadOnlyMemory<byte> charString)
        {
            charString = default;

            if (_isCid || (uint)standardCode > 255)
            {
                return false;
            }

            int sid = StandardEncodingSid(standardCode);
            int glyph = sid == 0 ? -1 : FindGlyphBySid(sid);

            if (glyph < 0)
            {
                return false;
            }

            charString = _charStrings[glyph];
            return true;
        }

        // The Standard Encoding (CFF spec, Appendix B) as code -> standard string SID; 0 is unencoded.
        // Codes 32..126 map to SIDs 1..95 in order, so only the upper half needs a table.
        private static int StandardEncodingSid(int code)
            => code is >= 32 and <= 126 ? code - 31
                : code >= 161 ? StandardEncodingUpperSids[code - 161]
                : 0;

        private static ReadOnlySpan<byte> StandardEncodingUpperSids => new byte[]
        {
            96, 97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 0, 111, 112, 113, 114,
            0, 115, 116, 117, 118, 119, 120, 121, 122, 0, 123, 0, 124, 125, 126, 127, 128, 129, 130, 131,
            0, 132, 133, 0, 134, 135, 136, 137, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 138, 0,
            139, 0, 0, 0, 0, 140, 141, 142, 143, 0, 0, 0, 0, 0, 144, 0, 0, 0, 145, 0, 0, 146, 147, 148, 149,
            0, 0, 0, 0
        };

        /// <summary>
        /// Finds the glyph whose charset entry is <paramref name="sid"/>, or -1. Scans the charset in
        /// place: seac is rare, so no reverse map is kept.
        /// </summary>
        private int FindGlyphBySid(int sid)
        {
            int glyphCount = _charStrings.Count;

            switch (_charset)
            {
                case 0:
                    // ISOAdobe: glyph i is SID i for SIDs 0..228.
                    return sid <= 228 && sid < glyphCount ? sid : -1;
                case 1:
                case 2:
                    // The Expert charsets name expert glyphs, which the Standard Encoding cannot address.
                    return -1;
            }

            var span = _table.Span;
            int p = _charset;

            if ((uint)p >= (uint)span.Length)
            {
                return -1;
            }

            byte format = span[p++];

            // Glyph 0 is always .notdef and has no charset entry.
            int glyph = 1;

            if (format == 0)
            {
                for (; glyph < glyphCount; glyph++, p += 2)
                {
                    if (p + 2 > span.Length)
                    {
                        return -1;
                    }

                    if (BinaryPrimitives.ReadUInt16BigEndian(span.Slice(p)) == sid)
                    {
                        return glyph;
                    }
                }

                return -1;
            }

            if (format is 1 or 2)
            {
                int leftSize = format == 1 ? 1 : 2;

                while (glyph < glyphCount)
                {
                    if (p + 2 + leftSize > span.Length)
                    {
                        return -1;
                    }

                    int first = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(p));
                    int left = leftSize == 1 ? span[p + 2] : BinaryPrimitives.ReadUInt16BigEndian(span.Slice(p + 2));
                    p += 2 + leftSize;

                    if (sid >= first && sid <= first + left)
                    {
                        int found = glyph + (sid - first);
                        return found < glyphCount ? found : -1;
                    }

                    glyph += left + 1;
                }
            }

            return -1;
        }

        /// <summary>
        /// Computes the control-point bounding box for <paramref name="glyphIndex"/> by interpreting
        /// its charstring into a bounds-accumulating sink. CFF stores no per-glyph bbox, so this is the
        /// equivalent of the <c>glyf</c> header box (an empty glyph yields the zero box). Returns
        /// <c>false</c> for an out-of-range glyph or a malformed charstring.
        /// </summary>
        public bool TryGetGlyphBounds(int glyphIndex, out GlyphBounds bounds)
        {
            bounds = default;

            var context = new BoundsGeometryContext();
            if (!TryBuildGlyphGeometry(glyphIndex, Matrix.Identity, context))
            {
                return false;
            }

            bounds = context.ToGlyphBounds();
            return true;
        }
    }
}
