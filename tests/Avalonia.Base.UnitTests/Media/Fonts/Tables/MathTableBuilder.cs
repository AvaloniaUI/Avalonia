using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.UnitTests;

namespace Avalonia.Base.UnitTests.Media.Fonts.Tables
{
    /// <summary>
    /// Builds MATH table data in memory. Every sub-table is written after <see cref="Padding"/> filler
    /// bytes, so each offset is relative to a parent that does not start at the table start, and a
    /// reader resolving an offset against the wrong parent reads filler instead of the record.
    /// </summary>
    internal sealed class MathTableBuilder
    {
        /// <summary>The number of constants in a MathConstants table.</summary>
        public const int ConstantCount = 56;

        private readonly int[] _constants = new int[ConstantCount];
        private readonly (int Outer, int Inner)?[] _constantVariations = new (int, int)?[ConstantCount];
        private readonly SortedDictionary<int, Value> _italics = new();
        private readonly SortedDictionary<int, Value> _accents = new();
        private readonly SortedSet<int> _extendedShapes = new();
        private readonly SortedDictionary<int, KernTable?[]> _kerns = new();
        private readonly SortedDictionary<int, Construction> _vertical = new();
        private readonly SortedDictionary<int, Construction> _horizontal = new();

        public int Padding { get; set; } = 6;

        public int MinorVersion { get; set; }

        /// <summary>Coverage format written for every coverage table, 1 or 2.</summary>
        public int CoverageFormat { get; set; } = 1;

        public int MinConnectorOverlap { get; set; }

        public bool IncludeConstants { get; set; } = true;

        public bool IncludeGlyphInfo { get; set; } = true;

        public bool IncludeVariants { get; set; } = true;

        public MathTableBuilder Constant(int index, int value, (int Outer, int Inner)? variation = null)
        {
            _constants[index] = value;
            _constantVariations[index] = variation;
            return this;
        }

        public MathTableBuilder ItalicsCorrection(int glyph, int value, (int Outer, int Inner)? variation = null)
        {
            _italics[glyph] = new Value(value, variation);
            return this;
        }

        public MathTableBuilder TopAccentAttachment(int glyph, int value, (int Outer, int Inner)? variation = null)
        {
            _accents[glyph] = new Value(value, variation);
            return this;
        }

        public MathTableBuilder ExtendedShape(int glyph)
        {
            _extendedShapes.Add(glyph);
            return this;
        }

        /// <summary>
        /// Sets the kern of one corner, in MathKernInfoRecord order: top right, top left, bottom
        /// right, bottom left. <paramref name="values"/> has one entry more than <paramref name="heights"/>.
        /// </summary>
        public MathTableBuilder Kern(int glyph, int corner, Value[] heights, Value[] values)
        {
            if (!_kerns.TryGetValue(glyph, out var corners))
            {
                _kerns[glyph] = corners = new KernTable?[4];
            }

            corners[corner] = new KernTable(heights, values);
            return this;
        }

        public MathTableBuilder Vertical(int glyph, (int Glyph, int Advance)[] variants, Assembly? assembly = null)
        {
            _vertical[glyph] = new Construction(variants, assembly);
            return this;
        }

        public MathTableBuilder Horizontal(int glyph, (int Glyph, int Advance)[] variants, Assembly? assembly = null)
        {
            _horizontal[glyph] = new Construction(variants, assembly);
            return this;
        }

        public byte[] Build()
        {
            var header = new Table().UInt16(1).UInt16(MinorVersion)
                .Offset(IncludeConstants ? BuildConstants() : null)
                .Offset(IncludeGlyphInfo ? BuildGlyphInfo() : null)
                .Offset(IncludeVariants ? BuildVariants() : null);

            return header.ToArray(Padding);
        }

        private Table BuildConstants()
        {
            var table = new Table();

            for (var i = 0; i < ConstantCount; i++)
            {
                switch (i)
                {
                    case 0:
                    case 1:
                    case 55:
                        table.Int16(_constants[i]);
                        break;
                    case 2:
                    case 3:
                        table.UInt16(_constants[i]);
                        break;
                    default:
                        table.Value(new Value(_constants[i], _constantVariations[i]));
                        break;
                }
            }

            return table;
        }

        private Table BuildGlyphInfo()
        {
            var table = new Table();

            table.Offset(_italics.Count > 0 ? BuildValueTable(_italics) : null);
            table.Offset(_accents.Count > 0 ? BuildValueTable(_accents) : null);
            table.Offset(_extendedShapes.Count > 0 ? Coverage(_extendedShapes) : null);

            if (_kerns.Count > 0)
            {
                var kernInfo = new Table().Offset(Coverage(_kerns.Keys)).UInt16(_kerns.Count);

                foreach (var corners in _kerns.Values)
                {
                    foreach (var kern in corners)
                    {
                        kernInfo.Offset(kern is null ? null : BuildKern(kern));
                    }
                }

                table.Offset(kernInfo);
            }
            else
            {
                table.Offset(null);
            }

            return table;
        }

        private Table BuildValueTable(SortedDictionary<int, Value> values)
        {
            var table = new Table().Offset(Coverage(values.Keys)).UInt16(values.Count);

            foreach (var value in values.Values)
            {
                table.Value(value);
            }

            return table;
        }

        private static Table BuildKern(KernTable kern)
        {
            var table = new Table().UInt16(kern.Heights.Length);

            foreach (var height in kern.Heights)
            {
                table.Value(height);
            }

            foreach (var value in kern.Values)
            {
                table.Value(value);
            }

            return table;
        }

        private Table BuildVariants()
        {
            var table = new Table().UInt16(MinConnectorOverlap)
                .Offset(_vertical.Count > 0 ? Coverage(_vertical.Keys) : null)
                .Offset(_horizontal.Count > 0 ? Coverage(_horizontal.Keys) : null)
                .UInt16(_vertical.Count)
                .UInt16(_horizontal.Count);

            foreach (var construction in _vertical.Values.Concat(_horizontal.Values))
            {
                table.Offset(BuildConstruction(construction));
            }

            return table;
        }

        private static Table BuildConstruction(Construction construction)
        {
            var table = new Table().Offset(construction.Assembly is { } assembly ? BuildAssembly(assembly) : null)
                .UInt16(construction.Variants.Length);

            foreach (var (glyph, advance) in construction.Variants)
            {
                table.UInt16(glyph).UInt16(advance);
            }

            return table;
        }

        private static Table BuildAssembly(Assembly assembly)
        {
            var table = new Table().Value(assembly.ItalicsCorrection).UInt16(assembly.Parts.Length);

            foreach (var part in assembly.Parts)
            {
                table.UInt16(part.Glyph).UInt16(part.StartConnector).UInt16(part.EndConnector)
                    .UInt16(part.FullAdvance).UInt16(part.Flags);
            }

            return table;
        }

        private Table Coverage(IEnumerable<int> glyphs)
        {
            var sorted = glyphs.OrderBy(g => g).ToArray();
            var table = new Table().UInt16(CoverageFormat);

            if (CoverageFormat == 1)
            {
                table.UInt16(sorted.Length);

                foreach (var glyph in sorted)
                {
                    table.UInt16(glyph);
                }

                return table;
            }

            var ranges = new List<(int Start, int End, int Index)>();

            for (var i = 0; i < sorted.Length; i++)
            {
                if (ranges.Count > 0 && ranges[^1].End == sorted[i] - 1)
                {
                    ranges[^1] = (ranges[^1].Start, sorted[i], ranges[^1].Index);
                }
                else
                {
                    ranges.Add((sorted[i], sorted[i], i));
                }
            }

            table.UInt16(ranges.Count);

            foreach (var (start, end, index) in ranges)
            {
                table.UInt16(start).UInt16(end).UInt16(index);
            }

            return table;
        }

        /// <summary>A MathValueRecord, optionally varied through a GDEF VariationIndex.</summary>
        public readonly record struct Value(int Design, (int Outer, int Inner)? Variation = null)
        {
            public static implicit operator Value(int design) => new(design);
        }

        public sealed record KernTable(Value[] Heights, Value[] Values);

        public sealed record Part(int Glyph, int StartConnector, int EndConnector, int FullAdvance, int Flags);

        public sealed record Assembly(Value ItalicsCorrection, Part[] Parts);

        private sealed record Construction((int Glyph, int Advance)[] Variants, Assembly? Assembly);

        /// <summary>
        /// A table's own bytes plus the child tables its Offset16 fields point to. Children are
        /// written after the table, each preceded by filler, and the offsets are patched relative to
        /// the start of this table.
        /// </summary>
        private sealed class Table
        {
            private readonly BigEndianBuffer _bytes = new();
            private readonly List<(int Position, Table Child)> _children = new();

            public Table UInt16(int value)
            {
                _bytes.UInt16(value);
                return this;
            }

            public Table Int16(int value)
            {
                _bytes.Int16(value);
                return this;
            }

            public Table Offset(Table? child)
            {
                if (child is not null)
                {
                    _children.Add((_bytes.Position, child));
                }

                _bytes.UInt16(0);
                return this;
            }

            public Table Value(Value value)
            {
                Int16(value.Design);

                // A VariationIndex table: deltaSetOuterIndex, deltaSetInnerIndex, deltaFormat 0x8000.
                return Offset(value.Variation is { } v
                    ? new Table().UInt16(v.Outer).UInt16(v.Inner).UInt16(0x8000)
                    : null);
            }

            public byte[] ToArray(int padding)
            {
                var output = new BigEndianBuffer().Bytes(_bytes.ToArray());

                foreach (var (position, child) in _children)
                {
                    for (var i = 0; i < padding; i++)
                    {
                        output.UInt8(0xEE);
                    }

                    var offset = output.Position;

                    if (offset > ushort.MaxValue)
                    {
                        throw new InvalidOperationException("MATH sub-table offset exceeds Offset16.");
                    }

                    output.Bytes(child.ToArray(padding));
                    output.PatchUInt16(position, offset);
                }

                return output.ToArray();
            }
        }
    }
}
