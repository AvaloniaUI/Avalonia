using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Platform;

namespace Avalonia.Media.Fonts.Tables.Colr
{
    /// <summary>
    /// Reader for the 'COLR' (Color) table. Provides access to layered color glyph data.
    /// Supports COLR v0 and v1 formats.
    /// </summary>
    internal sealed class ColrTable
    {
        internal const string TableName = "COLR";

        internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse(TableName);

        private readonly ReadOnlyMemory<byte> _colrData;
        private readonly ushort _version;
        private readonly ushort _numBaseGlyphRecords;
        private readonly uint _baseGlyphRecordsOffset;
        private readonly uint _layerRecordsOffset;
        private readonly ushort _numLayerRecords;

        // COLR v1 fields
        private readonly uint _baseGlyphV1ListOffset;
        private readonly uint _layerV1ListOffset;
        private readonly uint _clipListOffset;

        // Record counts clamped at load to the records that fit in the table, so lookups never read
        // past the data and record positions never overflow.
        private readonly int _numBaseGlyphV1Records;
        private readonly int _numLayerV1Records;
        private readonly int _numClips;

        // Cached variation tables (loaded during construction). These are the shared parsers under
        // Tables.Variation — the same ItemVariationStore HVAR / VVAR / MVAR use — which apply
        // region scaling against the instance's coords (the COLR-local copies did not).
        private readonly Variation.DeltaSetIndexMap? _deltaSetIndexMap;
        private readonly Variation.ItemVariationStore? _itemVariationStore;

        private ColrTable(
            ReadOnlyMemory<byte> colrData,
            ushort version,
            ushort numBaseGlyphRecords,
            uint baseGlyphRecordsOffset,
            uint layerRecordsOffset,
            ushort numLayerRecords,
            uint baseGlyphV1ListOffset = 0,
            uint layerV1ListOffset = 0,
            uint clipListOffset = 0,
            int numBaseGlyphV1Records = 0,
            int numLayerV1Records = 0,
            int numClips = 0,
            Variation.DeltaSetIndexMap? deltaSetIndexMap = null,
            Variation.ItemVariationStore? itemVariationStore = null)
        {
            _colrData = colrData;
            _version = version;
            _numBaseGlyphRecords = numBaseGlyphRecords;
            _baseGlyphRecordsOffset = baseGlyphRecordsOffset;
            _layerRecordsOffset = layerRecordsOffset;
            _numLayerRecords = numLayerRecords;
            _baseGlyphV1ListOffset = baseGlyphV1ListOffset;
            _layerV1ListOffset = layerV1ListOffset;
            _clipListOffset = clipListOffset;
            _numBaseGlyphV1Records = numBaseGlyphV1Records;
            _numLayerV1Records = numLayerV1Records;
            _numClips = numClips;
            _deltaSetIndexMap = deltaSetIndexMap;
            _itemVariationStore = itemVariationStore;
        }

        /// <summary>
        /// Gets the version of the COLR table (0 or 1).
        /// </summary>
        public ushort Version => _version;

        /// <summary>
        /// Gets the number of base glyph records (v0).
        /// </summary>
        public int BaseGlyphCount => _numBaseGlyphRecords;

        /// <summary>
        /// Gets whether this table has COLR v1 data.
        /// </summary>
        public bool HasV1Data => _version >= 1 && _baseGlyphV1ListOffset > 0;

        /// <summary>
        /// Gets the LayerV1List offset from the COLR table header.
        /// Returns 0 if the LayerV1List is not present (COLR v0 or no LayerV1List in v1).
        /// </summary>
        public uint LayerV1ListOffset => _layerV1ListOffset;

        /// <summary>
        /// Gets the number of paint offsets in the LayerV1List that fit in the table (0 when the list
        /// is absent).
        /// </summary>
        public int LayerV1Count => _numLayerV1Records;

        public ReadOnlyMemory<byte> ColrData => _colrData;

        /// <summary>
        /// Attempts to load the COLR (Color) table from the specified glyph typeface.
        /// </summary>
        /// <remarks>This method supports both COLR version 0 and version 1 tables, as defined in the
        /// OpenType specification. If the COLR table is not present or is invalid, the method returns false and sets
        /// colrTable to null.</remarks>
        /// <param name="glyphTypeface">The glyph typeface from which to load the COLR table. Cannot be null.</param>
        /// <param name="colrTable">When this method returns, contains the loaded COLR table if successful; otherwise, null. This parameter is
        /// passed uninitialized.</param>
        /// <returns>true if the COLR table was successfully loaded; otherwise, false.</returns>
        public static bool TryLoad(GlyphTypeface glyphTypeface, [NotNullWhen(true)] out ColrTable? colrTable)
        {
            colrTable = null;

            if (!glyphTypeface.PlatformTypeface.TryGetTable(Tag, out var colrData))
            {
                return false;
            }

            if (colrData.Length < 14)
            {
                return false; // Minimum size for COLR v0 header
            }

            var span = colrData.Span;

            // Parse COLR table header (v0)
            // uint16 version
            // uint16 numBaseGlyphRecords
            // Offset32 baseGlyphRecordsOffset
            // Offset32 layerRecordsOffset
            // uint16 numLayerRecords

            var version = BinaryPrimitives.ReadUInt16BigEndian(span);
            var numBaseGlyphRecords = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(2));
            var baseGlyphRecordsOffset = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(4));
            var layerRecordsOffset = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(8));
            var numLayerRecords = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(12));

            // Validate v0 offsets
            if (baseGlyphRecordsOffset >= colrData.Length || layerRecordsOffset >= colrData.Length)
            {
                return false;
            }

            // Parse COLR v1 extensions if present
            uint baseGlyphV1ListOffset = 0;
            uint layerV1ListOffset = 0;
            uint clipListOffset = 0;
            uint varIndexMapOffset = 0;
            uint itemVariationStoreOffset = 0;

            if (version >= 1)
            {
                // COLR v1 adds additional fields after the v0 header
                // Offset32 baseGlyphV1ListOffset (14 bytes)
                // Offset32 layerV1ListOffset (18 bytes)
                // Offset32 clipListOffset (22 bytes) - optional
                // Offset32 varIndexMapOffset (26 bytes) - optional
                // Offset32 itemVariationStoreOffset (30 bytes) - optional

                if (colrData.Length >= 22)
                {
                    baseGlyphV1ListOffset = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(14));
                    layerV1ListOffset = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(18));
                }

                if (colrData.Length >= 26)
                {
                    clipListOffset = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(22));
                }

                if (colrData.Length >= 30)
                {
                    varIndexMapOffset = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(26));
                }

                if (colrData.Length >= 34)
                {
                    itemVariationStoreOffset = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(30));
                }

                // Note: 0 offset means the optional table is not present
            }

            // A v1 subtable whose offset leaves no room for its header is treated as absent, and every
            // count is clamped to the records that fit, so a hostile header cannot drive a lookup past
            // the data.
            var numBaseGlyphV1Records = ClampRecordCount(span, ref baseGlyphV1ListOffset, headerSize: 4, recordSize: 6);
            var numLayerV1Records = ClampRecordCount(span, ref layerV1ListOffset, headerSize: 4, recordSize: 4);

            // ClipList: uint8 format (only 1 is defined), then uint32 numClips.
            if (clipListOffset != 0 && clipListOffset < span.Length && span[(int)clipListOffset] != 1)
            {
                clipListOffset = 0;
            }

            var numClips = ClampRecordCount(span, ref clipListOffset, headerSize: 5, recordSize: 7);

            // Load DeltaSetIndexMap if present (shared parser, data sliced to the subtable).
            Variation.DeltaSetIndexMap? deltaSetIndexMap = null;
            if (version >= 1 && varIndexMapOffset > 0 && varIndexMapOffset < colrData.Length)
            {
                Variation.DeltaSetIndexMap.TryLoad(colrData.Slice((int)varIndexMapOffset), out deltaSetIndexMap);
            }

            // Load ItemVariationStore if present. The shared store applies region scaling against the
            // instance coords, so it needs the fvar axis count to validate its region records.
            Variation.ItemVariationStore? itemVariationStore = null;
            if (version >= 1 && itemVariationStoreOffset > 0 && itemVariationStoreOffset < colrData.Length)
            {
                Variation.ItemVariationStore.TryLoad(
                    colrData.Slice((int)itemVariationStoreOffset),
                    glyphTypeface.VariationAxes.Count,
                    out itemVariationStore);
            }

            colrTable = new ColrTable(
                colrData,
                version,
                numBaseGlyphRecords,
                baseGlyphRecordsOffset,
                layerRecordsOffset,
                numLayerRecords,
                baseGlyphV1ListOffset,
                layerV1ListOffset,
                clipListOffset,
                numBaseGlyphV1Records,
                numLayerV1Records,
                numClips,
                deltaSetIndexMap,
                itemVariationStore);

            return true;
        }

        /// <summary>
        /// Validates a v1 subtable that starts with a uint32 record count (after
        /// <paramref name="headerSize"/> - 4 leading bytes) and returns how many of its fixed-size
        /// records fit in the table. An offset that leaves no room for the header is reset to 0.
        /// </summary>
        private static int ClampRecordCount(ReadOnlySpan<byte> table, ref uint offset, int headerSize, int recordSize)
        {
            if (offset == 0 || (long)offset + headerSize > table.Length)
            {
                offset = 0;
                return 0;
            }

            var subtable = table.Slice((int)offset);
            var count = BinaryPrimitives.ReadUInt32BigEndian(subtable.Slice(headerSize - 4));
            var fitting = (uint)((subtable.Length - headerSize) / recordSize);

            return (int)Math.Min(count, fitting);
        }

        /// <summary>
        /// Tries to find the base glyph record for the specified glyph ID (v0 format).
        /// Uses binary search for efficient lookup.
        /// </summary>
        public bool TryGetBaseGlyphRecord(ushort glyphIndex, out BaseGlyphRecord record)
        {
            record = default;

            if (_numBaseGlyphRecords == 0)
            {
                return false;
            }

            var span = _colrData.Span;
            var baseRecordsSpan = span.Slice((int)_baseGlyphRecordsOffset);

            // Binary search for the glyph ID
            int low = 0;
            int high = _numBaseGlyphRecords - 1;

            while (low <= high)
            {
                int mid = low + (high - low) / 2;
                int offset = mid * 6; // Each BaseGlyphRecord is 6 bytes

                if (offset + 6 > baseRecordsSpan.Length)
                {
                    return false;
                }

                var recordSpan = baseRecordsSpan.Slice(offset, 6);
                var recordGlyphId = BinaryPrimitives.ReadUInt16BigEndian(recordSpan);

                if (recordGlyphId == glyphIndex)
                {
                    // Found it
                    var firstLayerIndex = BinaryPrimitives.ReadUInt16BigEndian(recordSpan.Slice(2));
                    var numLayers = BinaryPrimitives.ReadUInt16BigEndian(recordSpan.Slice(4));

                    record = new BaseGlyphRecord(glyphIndex, firstLayerIndex, numLayers);
                    return true;
                }
                else if (recordGlyphId < glyphIndex)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the layer record at the specified index (v0 format).
        /// /// Gets the number of base glyph records (v0).
        /// /// Gets whether this table has COLR v1 data.
        /// /// Gets the LayerV1List offset from the COLR table header.
        /// Returns 0 if the LayerV1List is not present (COLR v0 or no LayerV1List in v1).
        /// /// </summary>
        public bool TryGetLayerRecord(int layerIndex, out LayerRecord record)
        {
            record = default;

            if (layerIndex < 0 || layerIndex >= _numLayerRecords)
            {
                return false;
            }

            var span = _colrData.Span;
            var layerRecordsSpan = span.Slice((int)_layerRecordsOffset);

            int offset = layerIndex * 4; // Each LayerRecord is 4 bytes

            if (offset + 4 > layerRecordsSpan.Length)
            {
                return false;
            }

            var recordSpan = layerRecordsSpan.Slice(offset, 4);
            var glyphIndex = BinaryPrimitives.ReadUInt16BigEndian(recordSpan);
            var paletteIndex = BinaryPrimitives.ReadUInt16BigEndian(recordSpan.Slice(2));

            record = new LayerRecord(glyphIndex, paletteIndex);
            return true;
        }

        /// <summary>
        /// Gets all layers for the specified glyph ID.
        /// Returns an empty array if the glyph has no color layers.
        /// </summary>
        public LayerRecord[] GetLayers(ushort glyphIndex)
        {
            if (!TryGetBaseGlyphRecord(glyphIndex, out var baseRecord))
            {
                return Array.Empty<LayerRecord>();
            }

            var layers = new LayerRecord[baseRecord.NumLayers];

            for (int i = 0; i < baseRecord.NumLayers; i++)
            {
                if (TryGetLayerRecord(baseRecord.FirstLayerIndex + i, out var layer))
                {
                    layers[i] = layer;
                }
            }

            return layers;
        }

        /// <summary>
        /// Tries to get the v1 base glyph record for the specified glyph ID.
        /// </summary>
        public bool TryGetBaseGlyphV1Record(ushort glyphIndex, out BaseGlyphV1Record record)
        {
            record = default;

            if (!HasV1Data)
            {
                return false;
            }

            // BaseGlyphV1List format:
            // uint32 numBaseGlyphV1Records
            // BaseGlyphV1Record[numBaseGlyphV1Records] (sorted by glyphID)
            // The count was clamped at load to the records that fit, so every record read is in range.
            var baseGlyphV1ListSpan = _colrData.Span.Slice((int)_baseGlyphV1ListOffset);

            int low = 0;
            int high = _numBaseGlyphV1Records - 1;
            int recordOffset = 4;

            while (low <= high)
            {
                int mid = low + (high - low) / 2;

                // 6 bytes per record: 2 (glyphID) + 4 (Offset32 paintOffset)
                int offset = recordOffset + (mid * 6);

                if (offset + 6 > baseGlyphV1ListSpan.Length)
                    return false;

                var recordSpan = baseGlyphV1ListSpan.Slice(offset, 6);
                var recordGlyphId = BinaryPrimitives.ReadUInt16BigEndian(recordSpan);

                if (recordGlyphId == glyphIndex)
                {
                    var paintOffset = BinaryPrimitives.ReadUInt32BigEndian(recordSpan.Slice(2));
                    record = new BaseGlyphV1Record(glyphIndex, paintOffset);
                    return true;
                }
                else if (recordGlyphId < glyphIndex)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return false;
        }

        /// <summary>
        /// Converts a paint offset from BaseGlyphV1Record (relative to BaseGlyphV1List) to an absolute offset within the COLR table.
        /// According to OpenType COLR v1 specification, offsets in BaseGlyphV1Records are relative to the start of the BaseGlyphV1List.
        /// </summary>
        /// <param name="relativePaintOffset">The paint offset from a BaseGlyphV1Record, relative to the BaseGlyphV1List.</param>
        /// <returns>The absolute offset within the COLR table.</returns>
        internal uint GetAbsolutePaintOffset(uint relativePaintOffset)
        {
            // According to the OpenType spec, paint offsets in BaseGlyphV1Records are
            // relative to the BaseGlyphV1List, so we add the BaseGlyphV1List offset to get
            // the absolute position in the COLR table
            // Saturate instead of wrapping, so an out-of-range sum fails the caller's bounds check.
            return (uint)Math.Min((ulong)_baseGlyphV1ListOffset + relativePaintOffset, uint.MaxValue);
        }

        /// <summary>
        /// Checks if the specified glyph has color layers defined (v0 or v1).
        /// </summary>
        public bool HasColorLayers(ushort glyphIndex)
        {
            // Check v0 first
            if (TryGetBaseGlyphRecord(glyphIndex, out _))
            {
                return true;
            }

            // Check v1
            if (HasV1Data && TryGetBaseGlyphV1Record(glyphIndex, out _))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Resolves the region-scaled variation delta for a single varying field of a variable paint
        /// record, at the instance's active coordinates.
        /// </summary>
        /// <param name="varIndexBase">The paint record's <c>VarIndexBase</c> (<c>0xFFFFFFFF</c> = no variation).</param>
        /// <param name="fieldOffset">The field's index offset from <paramref name="varIndexBase"/>.</param>
        /// <param name="coords">Normalized active coords in fvar axis order (empty at the default instance).</param>
        /// <param name="delta">The scaled delta in the field's raw units (caller applies the FWORD / F2DOT14 / Fixed scale).</param>
        /// <returns><c>true</c> if a delta was resolved; otherwise <c>false</c> (no variation, no store, or out of range).</returns>
        /// <remarks>
        /// The <c>(varIndexBase + fieldOffset)</c> variation index is mapped to an <c>(outer, inner)</c>
        /// pair via the DeltaSetIndexMap (or the implicit high/low-16-bit split when absent), then the
        /// shared <see cref="Variation.ItemVariationStore"/> sums <c>regionScaler(coords) × delta</c>
        /// across regions.
        /// </remarks>
        internal bool TryGetScaledDelta(uint varIndexBase, uint fieldOffset, ReadOnlySpan<float> coords, out float delta)
        {
            delta = 0f;

            const uint NoVariationIndex = 0xFFFFFFFF;

            if (varIndexBase == NoVariationIndex || _itemVariationStore == null)
            {
                return false;
            }

            var varIndex = varIndexBase + fieldOffset;

            int outerIndex;
            int innerIndex;

            if (_deltaSetIndexMap != null)
            {
                if (!_deltaSetIndexMap.TryGetIndices((int)varIndex, out outerIndex, out innerIndex))
                {
                    return false;
                }
            }
            else
            {
                // Implicit mapping per the OpenType spec: outer = high 16 bits, inner = low 16 bits.
                outerIndex = (int)(varIndex >> 16);
                innerIndex = (int)(varIndex & 0xFFFF);
            }

            return _itemVariationStore.TryGetDelta(outerIndex, innerIndex, coords, out delta);
        }

        /// <summary>
        /// Tries to get the clip box for a specified glyph ID from the ClipList (COLR v1).
        /// </summary>
        /// <param name="glyphIndex">The glyph ID to get the clip box for.</param>
        /// <param name="coords">Normalized active coords (empty at the default instance) for a variable (format 2) clip box.</param>
        /// <param name="clipBox">The clip box rectangle, or null if no clip box is defined.</param>
        /// <returns>True if a clip box was found; otherwise false.</returns>
        public bool TryGetClipBox(ushort glyphIndex, ReadOnlySpan<float> coords, out Rect clipBox)
        {
            clipBox = default;

            // ClipList is only available in COLR v1. Its format and record count were validated and
            // clamped at load.
            if (!HasV1Data || _numClips == 0)
            {
                return false;
            }

            var span = _colrData.Span;
            var clipListSpan = span.Slice((int)_clipListOffset);

            // ClipList format:
            // uint8 format (must be 1)
            // uint32 numClips
            // ClipRecord[numClips] (sorted by startGlyphID)

            // Binary search for the clip record
            // ClipRecord format:
            // uint16 startGlyphID
            // uint16 endGlyphID
            // Offset24 clipBoxOffset (relative to start of ClipList)

            int recordSize = 7; // 2 + 2 + 3
            int low = 0;
            int high = _numClips - 1;
            int recordsOffset = 5; // After format + numClips

            while (low <= high)
            {
                int mid = low + (high - low) / 2;
                int offset = recordsOffset + (mid * recordSize);

                if (offset + recordSize > clipListSpan.Length)
                {
                    return false;
                }

                var recordSpan = clipListSpan.Slice(offset, recordSize);
                var startGlyphId = BinaryPrimitives.ReadUInt16BigEndian(recordSpan);
                var endGlyphId = BinaryPrimitives.ReadUInt16BigEndian(recordSpan.Slice(2));

                if (glyphIndex >= startGlyphId && glyphIndex <= endGlyphId)
                {
                    // Found the clip record - parse the clip box
                    var clipBoxOffset = ReadOffset24(recordSpan.Slice(4));
                    var absoluteClipBoxOffset = _clipListOffset + clipBoxOffset;

                    return TryParseClipBox(span, absoluteClipBoxOffset, coords, out clipBox);
                }
                else if (glyphIndex < startGlyphId)
                {
                    high = mid - 1;
                }
                else
                {
                    low = mid + 1;
                }
            }

            return false;
        }

        /// <summary>
        /// Tries to parse a ClipBox from the specified offset. Format 1 is a fixed box (four FWORDs);
        /// format 2 adds a single <c>varIndexBase</c>, with each corner varying at
        /// <c>varIndexBase + 0..3</c> (FWORD deltas) at the supplied coordinates. Coordinates stay in
        /// font space (Y-up); the Y-flip is applied by the Draw / Bounds path.
        /// </summary>
        private bool TryParseClipBox(ReadOnlySpan<byte> data, uint offset, ReadOnlySpan<float> coords, out Rect clipBox)
        {
            clipBox = default;

            if (offset >= data.Length)
            {
                return false;
            }

            var span = data.Slice((int)offset);

            // Both formats start with uint8 format + FWORD xMin/yMin/xMax/yMax.
            if (span.Length < 9)
            {
                return false;
            }

            var format = span[0];

            if (format != 1 && format != 2)
            {
                return false;
            }

            double xMin = BinaryPrimitives.ReadInt16BigEndian(span.Slice(1));
            double yMin = BinaryPrimitives.ReadInt16BigEndian(span.Slice(3));
            double xMax = BinaryPrimitives.ReadInt16BigEndian(span.Slice(5));
            double yMax = BinaryPrimitives.ReadInt16BigEndian(span.Slice(7));

            if (format == 2)
            {
                // Format 2: a single uint32 varIndexBase follows the four FWORDs; the corners are FWORD
                // deltas at base + 0..3.
                if (span.Length < 13)
                {
                    return false;
                }

                var varIndexBase = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(9));

                if (TryGetScaledDelta(varIndexBase, 0, coords, out var dxMin)) xMin += dxMin;
                if (TryGetScaledDelta(varIndexBase, 1, coords, out var dyMin)) yMin += dyMin;
                if (TryGetScaledDelta(varIndexBase, 2, coords, out var dxMax)) xMax += dxMax;
                if (TryGetScaledDelta(varIndexBase, 3, coords, out var dyMax)) yMax += dyMax;
            }

            clipBox = new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
            return true;
        }

        /// <summary>
        /// Reads a 24-bit offset (3 bytes, big-endian).
        /// </summary>
        private static uint ReadOffset24(ReadOnlySpan<byte> span)
        {
            return ((uint)span[0] << 16) | ((uint)span[1] << 8) | span[2];
        }
    }

    /// <summary>
    /// Represents a base glyph record in the COLR table (v0).
    /// Maps a glyph ID to its color layers.
    /// </summary>
    internal readonly struct BaseGlyphRecord
    {
        public BaseGlyphRecord(ushort glyphIndex, ushort firstLayerIndex, ushort numLayers)
        {
            GlyphIndex = glyphIndex;
            FirstLayerIndex = firstLayerIndex;
            NumLayers = numLayers;
        }

        /// <summary>
        /// Gets the glyph ID of the base glyph.
        /// </summary>
        public ushort GlyphIndex { get; }

        /// <summary>
        /// Gets the index of the first layer record for this glyph.
        /// </summary>
        public ushort FirstLayerIndex { get; }

        /// <summary>
        /// Gets the number of color layers for this glyph.
        /// </summary>
        public ushort NumLayers { get; }
    }

    /// <summary>
    /// Represents a v1 base glyph record in the COLR table.
    /// Maps a glyph ID to a paint offset.
    /// </summary>
    internal readonly struct BaseGlyphV1Record
    {
        public BaseGlyphV1Record(ushort glyphIndex, uint paintOffset)
        {
            GlyphIndex = glyphIndex;
            PaintOffset = paintOffset;
        }

        /// <summary>
        /// Gets the glyph ID of the base glyph.
        /// </summary>
        public ushort GlyphIndex { get; }

        /// <summary>
        /// Gets the offset to the paint table for this glyph.
        /// </summary>
        public uint PaintOffset { get; }
    }

    /// <summary>
    /// Represents a layer record in the COLR table (v0).
    /// Each layer references a glyph and a color palette index.
    /// </summary>
    internal readonly struct LayerRecord
    {
        public LayerRecord(ushort glyphIndex, ushort paletteIndex)
        {
            GlyphIndex = glyphIndex;
            PaletteIndex = paletteIndex;
        }

        /// <summary>
        /// Gets the glyph ID for this layer.
        /// This typically references a glyph in the 'glyf' or 'CFF' table.
        /// </summary>
        public ushort GlyphIndex { get; }

        /// <summary>
        /// Gets the color palette index for this layer.
        /// References a color in the CPAL (Color Palette) table.
        /// </summary>
        public ushort PaletteIndex { get; }
    }
}
