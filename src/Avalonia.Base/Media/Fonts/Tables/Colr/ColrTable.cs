using System;
using System.Buffers.Binary;
using System.Diagnostics;
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
        private readonly uint _varIndexMapOffset;
        private readonly uint _itemVariationStoreOffset;

        // Cached DeltaSetIndexMap (loaded during construction)
        private readonly DeltaSetIndexMap? _deltaSetIndexMap;

        // Cached ItemVariationStore (loaded during construction)
        private readonly ItemVariationStore? _itemVariationStore;

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
            uint varIndexMapOffset = 0,
            uint itemVariationStoreOffset = 0,
            DeltaSetIndexMap? deltaSetIndexMap = null,
            ItemVariationStore? itemVariationStore = null)
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
            _varIndexMapOffset = varIndexMapOffset;
            _itemVariationStoreOffset = itemVariationStoreOffset;
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

            // Load DeltaSetIndexMap if present
            DeltaSetIndexMap? deltaSetIndexMap = null;
            if (version >= 1 && varIndexMapOffset > 0)
            {
                deltaSetIndexMap = DeltaSetIndexMap.Load(colrData, varIndexMapOffset);
            }

            // Load ItemVariationStore if present
            ItemVariationStore? itemVariationStore = null;
            if (version >= 1 && itemVariationStoreOffset > 0)
            {
                itemVariationStore = ItemVariationStore.Load(colrData, itemVariationStoreOffset);
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
                varIndexMapOffset,
                itemVariationStoreOffset,
                deltaSetIndexMap,
                itemVariationStore);

            return true;
        }

        /// <summary>
        /// Tries to find the base glyph record for the specified glyph ID (v0 format).
        /// Uses binary search for efficient lookup.
        /// </summary>
        public bool TryGetBaseGlyphRecord(ushort glyphId, out BaseGlyphRecord record)
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

                if (recordGlyphId == glyphId)
                {
                    // Found it
                    var firstLayerIndex = BinaryPrimitives.ReadUInt16BigEndian(recordSpan.Slice(2));
                    var numLayers = BinaryPrimitives.ReadUInt16BigEndian(recordSpan.Slice(4));

                    record = new BaseGlyphRecord(glyphId, firstLayerIndex, numLayers);
                    return true;
                }
                else if (recordGlyphId < glyphId)
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
            var glyphId = BinaryPrimitives.ReadUInt16BigEndian(recordSpan);
            var paletteIndex = BinaryPrimitives.ReadUInt16BigEndian(recordSpan.Slice(2));

            record = new LayerRecord(glyphId, paletteIndex);
            return true;
        }

        /// <summary>
        /// Gets all layers for the specified glyph ID.
        /// Returns an empty array if the glyph has no color layers.
        /// </summary>
        public LayerRecord[] GetLayers(ushort glyphId)
        {
            if (!TryGetBaseGlyphRecord(glyphId, out var baseRecord))
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
        public bool TryGetBaseGlyphV1Record(ushort glyphId, out BaseGlyphV1Record record)
        {
            record = default;

            if (!HasV1Data)
            {
                return false;
            }

            var span = _colrData.Span;
            var baseGlyphV1ListSpan = span.Slice((int)_baseGlyphV1ListOffset);

            // BaseGlyphV1List format:
            // uint32 numBaseGlyphV1Records
            // BaseGlyphV1Record[numBaseGlyphV1Records] (sorted by glyphID)

            if (baseGlyphV1ListSpan.Length < 4)
            {
                return false;
            }

            var numRecords = BinaryPrimitives.ReadUInt32BigEndian(baseGlyphV1ListSpan);

            Debug.Assert(4 + numRecords * 6 <= _colrData.Length - _baseGlyphV1ListOffset);

            int low = 0;
            int high = (int)numRecords - 1;
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

                if (recordGlyphId == glyphId)
                {
                    var paintOffset = BinaryPrimitives.ReadUInt32BigEndian(recordSpan.Slice(2));
                    record = new BaseGlyphV1Record(glyphId, paintOffset);
                    return true;
                }
                else if (recordGlyphId < glyphId)
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
            return _baseGlyphV1ListOffset + relativePaintOffset;
        }

        /// <summary>
        /// Attempts to resolve and retrieve the paint definition for the specified glyph, if available.
        /// </summary>
        /// <remarks>This method returns <see langword="false"/> if the glyph does not have a version 1
        /// base glyph record or if the paint cannot be parsed or resolved. The output parameter <paramref
        /// name="paint"/> is set only when the method returns <see langword="true"/>.</remarks>
        /// <param name="context">The context containing color and font information used to resolve the paint.</param>
        /// <param name="glyphId">The identifier of the glyph for which to retrieve the resolved paint.</param>
        /// <param name="paint">When this method returns, contains the resolved paint for the specified glyph if the operation succeeds;
        /// otherwise, <see langword="null"/>. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the resolved paint was successfully retrieved; otherwise, <see langword="false"/>.</returns>
        public bool TryGetResolvedPaint(ColrContext context, ushort glyphId, [NotNullWhen(true)] out Paint? paint)
        {
            paint = null;

            if (!TryGetBaseGlyphV1Record(glyphId, out var record))
            {
                return false;
            }

            var absolutePaintOffset = GetAbsolutePaintOffset(record.PaintOffset);

            var decycler = PaintDecycler.Rent();
            try
            {
                if (!PaintParser.TryParse(_colrData.Span, absolutePaintOffset, in context, in decycler, out var parsedPaint))
                {
                    return false;
                }

                paint = PaintResolver.ResolvePaint(parsedPaint, in context);

                return true;
            }
            finally
            {
                PaintDecycler.Return(decycler);
            }
        }

        /// <summary>
        /// Checks if the specified glyph has color layers defined (v0 or v1).
        /// </summary>
        public bool HasColorLayers(ushort glyphId)
        {
            // Check v0 first
            if (TryGetBaseGlyphRecord(glyphId, out _))
            {
                return true;
            }

            // Check v1
            if (HasV1Data && TryGetBaseGlyphV1Record(glyphId, out _))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to get a complete delta set for the specified variation index.
        /// </summary>
        /// <param name="variationIndex">The index to look up in the DeltaSetIndexMap.</param>
        /// <param name="deltaSet">A DeltaSet ref struct providing format-aware access to deltas.</param>
        /// <returns>True if variation deltas were found; otherwise false.</returns>
        /// <remarks>
        /// This method uses the DeltaSetIndexMap to map the variation index to an (outer, inner) index pair,
        /// then retrieves the corresponding delta set from the ItemVariationStore.
        /// If no DeltaSetIndexMap is present, an implicit 1:1 mapping is used.
        /// The DeltaSet ref struct provides allocation-free access to both word deltas (16-bit) and byte deltas (8-bit).
        /// </remarks>
        public bool TryGetVariationDeltaSet(uint variationIndex, out DeltaSet deltaSet)
        {
            deltaSet = DeltaSet.Empty;

            // Magic value 0xFFFFFFFF indicates no variation deltas should be applied
            const uint NO_VARIATION_DELTAS = 0xFFFFFFFF;

            if (variationIndex == NO_VARIATION_DELTAS)
            {
                return false;
            }

            // ItemVariationStore is required
            if (!HasV1Data || _itemVariationStore == null)
            {
                return false;
            }

            // If DeltaSetIndexMap is present, use it for mapping
            if (_deltaSetIndexMap != null)
            {
                return _deltaSetIndexMap.TryGetVariationDeltaSet(_itemVariationStore, variationIndex, out deltaSet);
            }

            // Otherwise, use implicit 1:1 mapping as per OpenType spec
            if (variationIndex > ushort.MaxValue)
            {
                return false; // Index too large for implicit mapping
            }

            var implicitOuterIndex = (ushort)variationIndex;
            var implicitInnerIndex = (ushort)variationIndex;

            return _itemVariationStore.TryGetDeltaSet(implicitOuterIndex, implicitInnerIndex, out deltaSet);
        }

        /// <summary>
        /// Tries to get the clip box for a specified glyph ID from the ClipList (COLR v1).
        /// </summary>
        /// <param name="glyphId">The glyph ID to get the clip box for.</param>
        /// <param name="clipBox">The clip box rectangle, or null if no clip box is defined.</param>
        /// <returns>True if a clip box was found; otherwise false.</returns>
        public bool TryGetClipBox(ushort glyphId, out Rect clipBox)
        {
            clipBox = default;

            // ClipList is only available in COLR v1
            if (!HasV1Data || _clipListOffset == 0)
            {
                return false;
            }

            var span = _colrData.Span;

            if (_clipListOffset >= span.Length)
            {
                return false;
            }

            var clipListSpan = span.Slice((int)_clipListOffset);

            // ClipList format:
            // uint8 format (must be 1)
            // uint32 numClips
            // ClipRecord[numClips] (sorted by startGlyphID)

            if (clipListSpan.Length < 5) // format (1) + numClips (4)
            {
                return false;
            }

            var format = clipListSpan[0];
            if (format != 1)
            {
                return false; // Only format 1 is defined
            }

            var numClips = BinaryPrimitives.ReadUInt32BigEndian(clipListSpan.Slice(1));

            if (numClips == 0)
            {
                return false;
            }

            // Binary search for the clip record
            // ClipRecord format:
            // uint16 startGlyphID
            // uint16 endGlyphID
            // Offset24 clipBoxOffset (relative to start of ClipList)

            int recordSize = 7; // 2 + 2 + 3
            int low = 0;
            int high = (int)numClips - 1;
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

                if (glyphId >= startGlyphId && glyphId <= endGlyphId)
                {
                    // Found the clip record - parse the clip box
                    var clipBoxOffset = ReadOffset24(recordSpan.Slice(4));
                    var absoluteClipBoxOffset = _clipListOffset + clipBoxOffset;

                    return TryParseClipBox(span, absoluteClipBoxOffset, out clipBox);
                }
                else if (glyphId < startGlyphId)
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
        /// Tries to parse a ClipBox from the specified offset.
        /// </summary>
        private static bool TryParseClipBox(ReadOnlySpan<byte> data, uint offset, out Rect clipBox)
        {
            clipBox = default;

            if (offset >= data.Length)
            {
                return false;
            }

            var span = data.Slice((int)offset);

            // ClipBox format (format 1 or 2):
            // uint8 format
            // For format 1 (FWORD values):
            //   FWORD xMin
            //   FWORD yMin
            //   FWORD xMax
            //   FWORD yMax
            // For format 2 (FWORD + VarIndexBase for each):
            //   FWORD xMin, uint32 varIndexBase
            //   FWORD yMin, uint32 varIndexBase
            //   FWORD xMax, uint32 varIndexBase
            //   FWORD yMax, uint32 varIndexBase

            if (span.Length < 1)
            {
                return false;
            }

            var format = span[0];

            if (format == 1)
            {
                // Format 1: Fixed ClipBox
                if (span.Length < 9) // format (1) + 4 FWORDs (8)
                {
                    return false;
                }

                var xMin = BinaryPrimitives.ReadInt16BigEndian(span.Slice(1));
                var yMin = BinaryPrimitives.ReadInt16BigEndian(span.Slice(3));
                var xMax = BinaryPrimitives.ReadInt16BigEndian(span.Slice(5));
                var yMax = BinaryPrimitives.ReadInt16BigEndian(span.Slice(7));

                // Keep in font-space coordinates (Y-up)
                // The Y-flip transformation is applied at the root level in PaintResolver
                clipBox = new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
                return true;
            }
            else if (format == 2)
            {
                // Format 2: Variable ClipBox (ignore VarIndexBase for now)
                if (span.Length < 25) // format (1) + 4 * (FWORD (2) + VarIndexBase (4))
                {
                    return false;
                }

                var xMin = BinaryPrimitives.ReadInt16BigEndian(span.Slice(1));
                // Skip VarIndexBase at span[3..7]
                var yMin = BinaryPrimitives.ReadInt16BigEndian(span.Slice(7));
                // Skip VarIndexBase at span[9..13]
                var xMax = BinaryPrimitives.ReadInt16BigEndian(span.Slice(13));
                // Skip VarIndexBase at span[15..19]
                var yMax = BinaryPrimitives.ReadInt16BigEndian(span.Slice(19));
                // Skip VarIndexBase at span[21..25]

                // Keep in font-space coordinates (Y-up)
                // The Y-flip transformation is applied at the root level in PaintResolver
                clipBox = new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
                return true;
            }

            return false;
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
        public BaseGlyphRecord(ushort glyphId, ushort firstLayerIndex, ushort numLayers)
        {
            GlyphId = glyphId;
            FirstLayerIndex = firstLayerIndex;
            NumLayers = numLayers;
        }

        /// <summary>
        /// Gets the glyph ID of the base glyph.
        /// </summary>
        public ushort GlyphId { get; }

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
        public BaseGlyphV1Record(ushort glyphId, uint paintOffset)
        {
            GlyphId = glyphId;
            PaintOffset = paintOffset;
        }

        /// <summary>
        /// Gets the glyph ID of the base glyph.
        /// </summary>
        public ushort GlyphId { get; }

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
        public LayerRecord(ushort glyphId, ushort paletteIndex)
        {
            GlyphId = glyphId;
            PaletteIndex = paletteIndex;
        }

        /// <summary>
        /// Gets the glyph ID for this layer.
        /// This typically references a glyph in the 'glyf' or 'CFF' table.
        /// </summary>
        public ushort GlyphId { get; }

        /// <summary>
        /// Gets the color palette index for this layer.
        /// References a color in the CPAL (Color Palette) table.
        /// </summary>
        public ushort PaletteIndex { get; }
    }
}
