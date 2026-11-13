using System;
using System.Buffers.Binary;
using System.Threading;
using Avalonia.Media.Fonts.Tables.Variation;

namespace Avalonia.Media.Fonts.Tables.OpenTypeMath
{
    /// <summary>
    /// Reads the MathVariants sub-table: the size variants and glyph assemblies through which
    /// delimiters, radicals, large operators and wide accents grow.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Variant advances and glyph part lengths are plain design-unit values with no device records,
    /// so they are the same at every variation instance. Each construction's variants and parts are
    /// decoded on first request and cached by coverage index, shared by all instances of the font.
    /// Only an assembly's italics correction is a MathValueRecord and is resolved per call.
    /// </para>
    /// <para>
    /// The cache entries are published without a lock: two threads decoding the same construction
    /// build equal arrays, and either may win.
    /// </para>
    /// </remarks>
    internal sealed class MathVariantsTable
    {
        private const int HeaderSize = 10;
        private const int ConstructionHeaderSize = 4;
        private const int VariantRecordSize = 4;
        private const int AssemblyHeaderSize = 6;
        private const int PartRecordSize = 10;
        private const ushort ExtenderFlag = 0x0001;

        private readonly ReadOnlyMemory<byte> _table;
        private readonly Direction _vertical;
        private readonly Direction _horizontal;

        private MathVariantsTable(ReadOnlyMemory<byte> table, int minConnectorOverlap, Direction vertical, Direction horizontal)
        {
            _table = table;
            MinConnectorOverlap = minConnectorOverlap;
            _vertical = vertical;
            _horizontal = horizontal;
        }

        /// <summary>
        /// Gets the minimum overlap of connecting glyph parts in an assembly, in design units.
        /// </summary>
        public int MinConnectorOverlap { get; }

        /// <summary>
        /// Loads the table from <paramref name="table"/>, the bytes from the start of MathVariants to
        /// the end of the MATH table.
        /// </summary>
        /// <returns><c>null</c> when the header or the construction offset arrays are truncated.</returns>
        public static MathVariantsTable? Load(ReadOnlyMemory<byte> table)
        {
            if (table.Length < HeaderSize)
            {
                return null;
            }

            var span = table.Span;
            int verticalCount = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(6));
            int horizontalCount = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(8));

            if (HeaderSize + (verticalCount + horizontalCount) * 2 > table.Length)
            {
                return null;
            }

            return new MathVariantsTable(
                table,
                BinaryPrimitives.ReadUInt16BigEndian(span),
                Direction.Load(table, BinaryPrimitives.ReadUInt16BigEndian(span.Slice(2)), verticalCount, HeaderSize),
                Direction.Load(table, BinaryPrimitives.ReadUInt16BigEndian(span.Slice(4)), horizontalCount,
                    HeaderSize + verticalCount * 2));
        }

        /// <summary>
        /// Gets the size variants of <paramref name="glyph"/> in <paramref name="direction"/>, in the
        /// font's order of increasing size, or an empty span when the glyph has none.
        /// </summary>
        public ReadOnlySpan<MathGlyphVariant> GetGlyphVariants(ushort glyph, MathDirection direction)
        {
            var target = direction == MathDirection.Vertical ? _vertical : _horizontal;

            if (!TryGetConstruction(target, glyph, out var index, out var construction))
            {
                return default;
            }

            var cached = Volatile.Read(ref target.Variants[index]);

            if (cached is null)
            {
                cached = DecodeVariants(construction);
                Volatile.Write(ref target.Variants[index], cached);
            }

            return cached;
        }

        /// <summary>
        /// Gets the glyph assembly of <paramref name="glyph"/> in <paramref name="direction"/>.
        /// </summary>
        /// <returns><c>false</c> when the glyph has no construction or the construction no assembly.</returns>
        public bool TryGetGlyphAssembly(
            ushort glyph,
            MathDirection direction,
            ItemVariationStore? store,
            ReadOnlySpan<float> regionScalers,
            out MathGlyphAssembly assembly)
        {
            assembly = default;

            var target = direction == MathDirection.Vertical ? _vertical : _horizontal;

            if (!TryGetConstruction(target, glyph, out var index, out var construction))
            {
                return false;
            }

            var constructionSpan = construction.Span;
            int assemblyOffset = BinaryPrimitives.ReadUInt16BigEndian(constructionSpan);

            if (assemblyOffset == 0 || assemblyOffset > construction.Length - AssemblyHeaderSize)
            {
                return false;
            }

            var assemblySpan = constructionSpan.Slice(assemblyOffset);
            int partCount = BinaryPrimitives.ReadUInt16BigEndian(assemblySpan.Slice(4));

            if (AssemblyHeaderSize + partCount * PartRecordSize > assemblySpan.Length)
            {
                return false;
            }

            var parts = Volatile.Read(ref target.Parts[index]);

            if (parts is null)
            {
                parts = DecodeParts(assemblySpan, partCount);
                Volatile.Write(ref target.Parts[index], parts);
            }

            assembly = new MathGlyphAssembly(MathValueRecord.Read(assemblySpan, 0, store, regionScalers), parts);

            return true;
        }

        private bool TryGetConstruction(Direction direction, ushort glyph, out int index, out ReadOnlyMemory<byte> construction)
        {
            construction = default;
            index = direction.Coverage?.GetCoverageIndex(glyph) ?? -1;

            if ((uint)index >= (uint)direction.Count)
            {
                return false;
            }

            int offset = BinaryPrimitives.ReadUInt16BigEndian(_table.Span.Slice(direction.OffsetsStart + index * 2));

            if (offset == 0 || offset > _table.Length - ConstructionHeaderSize)
            {
                return false;
            }

            construction = _table.Slice(offset);

            return true;
        }

        private static MathGlyphVariant[] DecodeVariants(ReadOnlyMemory<byte> construction)
        {
            var span = construction.Span;
            int count = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(2));

            if (count == 0 || ConstructionHeaderSize + count * VariantRecordSize > span.Length)
            {
                return Array.Empty<MathGlyphVariant>();
            }

            var variants = new MathGlyphVariant[count];

            for (var i = 0; i < count; i++)
            {
                var record = span.Slice(ConstructionHeaderSize + i * VariantRecordSize);

                variants[i] = new MathGlyphVariant(
                    BinaryPrimitives.ReadUInt16BigEndian(record),
                    BinaryPrimitives.ReadUInt16BigEndian(record.Slice(2)));
            }

            return variants;
        }

        private static MathGlyphPart[] DecodeParts(ReadOnlySpan<byte> assembly, int count)
        {
            if (count == 0)
            {
                return Array.Empty<MathGlyphPart>();
            }

            var parts = new MathGlyphPart[count];

            for (var i = 0; i < count; i++)
            {
                var record = assembly.Slice(AssemblyHeaderSize + i * PartRecordSize);

                parts[i] = new MathGlyphPart(
                    BinaryPrimitives.ReadUInt16BigEndian(record),
                    BinaryPrimitives.ReadUInt16BigEndian(record.Slice(2)),
                    BinaryPrimitives.ReadUInt16BigEndian(record.Slice(4)),
                    BinaryPrimitives.ReadUInt16BigEndian(record.Slice(6)),
                    (BinaryPrimitives.ReadUInt16BigEndian(record.Slice(8)) & ExtenderFlag) != 0);
            }

            return parts;
        }

        /// <summary>
        /// The coverage and construction offsets of one direction, with the decoded variants and parts
        /// cached by coverage index.
        /// </summary>
        private sealed class Direction
        {
            private static readonly Direction s_empty = new(null, 0, 0);

            private Direction(CoverageTable? coverage, int count, int offsetsStart)
            {
                Coverage = coverage;
                Count = coverage is null ? 0 : count;
                OffsetsStart = offsetsStart;
                Variants = new MathGlyphVariant[]?[Count];
                Parts = new MathGlyphPart[]?[Count];
            }

            public CoverageTable? Coverage { get; }

            public int Count { get; }

            public int OffsetsStart { get; }

            public MathGlyphVariant[]?[] Variants { get; }

            public MathGlyphPart[]?[] Parts { get; }

            public static Direction Load(ReadOnlyMemory<byte> table, int coverageOffset, int count, int offsetsStart)
            {
                if (coverageOffset == 0 || count == 0 || !CoverageTable.TryLoad(table, coverageOffset, out var coverage))
                {
                    return s_empty;
                }

                return new Direction(coverage, count, offsetsStart);
            }
        }
    }
}
