using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Media.Fonts.Tables.Variation;

namespace Avalonia.Media.Fonts.Tables.OpenTypeMath
{
    /// <summary>
    /// Reads the OpenType 'MATH' table: the layout constants, per-glyph information and glyph
    /// variants a math layout engine needs from a math font.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The parsed table is shared by every variation instance of a font. Methods that return
    /// MathValueRecords take the GDEF item variation store and the instance's region scalers; an
    /// empty scaler span returns the values as designed.
    /// </para>
    /// <para>
    /// Reference: <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/math"/>.
    /// </para>
    /// </remarks>
    internal sealed class MathTable
    {
        internal const string TableName = "MATH";
        internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse(TableName);

        /// <summary>
        /// The number of constants in the MathConstants table, one per <see cref="MathConstant"/>.
        /// </summary>
        public const int ConstantCount = (int)MathConstant.RadicalDegreeBottomRaisePercent + 1;

        private const ushort SupportedMajorVersion = 1;
        private const int HeaderSize = 10;

        // MathConstants: two int16 percentages and two UFWORD heights, then 51 MathValueRecords
        // from MathLeading to RadicalKernAfterDegree, then the int16 RadicalDegreeBottomRaisePercent.
        private const int FirstValueRecordOffset = 8;
        private const int ValueRecordCount = 51;
        private const int ConstantsSize = FirstValueRecordOffset + ValueRecordCount * MathValueRecord.Size + 2;

        private readonly ReadOnlyMemory<byte> _constants;

        private MathTable(ReadOnlyMemory<byte> constants, MathGlyphInfoTable? glyphInfo, MathVariantsTable? variants)
        {
            _constants = constants;
            GlyphInfo = glyphInfo;
            Variants = variants;
        }

        /// <summary>
        /// Gets the MathGlyphInfo sub-table, or <c>null</c> when it is absent or truncated.
        /// </summary>
        public MathGlyphInfoTable? GlyphInfo { get; }

        /// <summary>
        /// Gets the MathVariants sub-table, or <c>null</c> when it is absent or truncated.
        /// </summary>
        public MathVariantsTable? Variants { get; }

        /// <summary>
        /// Gets whether the table has a usable MathConstants sub-table. Without one every constant is 0.
        /// </summary>
        public bool HasConstants => !_constants.IsEmpty;

        public static bool TryLoad(GlyphTypeface glyphTypeface, [NotNullWhen(true)] out MathTable? mathTable)
        {
            mathTable = null;

            return glyphTypeface.PlatformTypeface.TryGetTable(Tag, out var data) && TryLoad(data, out mathTable);
        }

        /// <returns>
        /// <c>false</c> when the header is truncated, its major version is not 1, or none of the three
        /// sub-tables is usable.
        /// </returns>
        public static bool TryLoad(ReadOnlyMemory<byte> data, [NotNullWhen(true)] out MathTable? mathTable)
        {
            mathTable = null;

            var span = data.Span;

            if (span.Length < HeaderSize || BinaryPrimitives.ReadUInt16BigEndian(span) != SupportedMajorVersion)
            {
                return false;
            }

            int constantsOffset = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(4));
            int glyphInfoOffset = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(6));
            int variantsOffset = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(8));

            var constants = constantsOffset != 0 && constantsOffset <= data.Length - ConstantsSize
                ? data.Slice(constantsOffset)
                : default;

            var glyphInfo = glyphInfoOffset != 0 && glyphInfoOffset < data.Length
                ? MathGlyphInfoTable.Load(data.Slice(glyphInfoOffset))
                : null;

            var variants = variantsOffset != 0 && variantsOffset < data.Length
                ? MathVariantsTable.Load(data.Slice(variantsOffset))
                : null;

            if (constants.IsEmpty && glyphInfo is null && variants is null)
            {
                return false;
            }

            mathTable = new MathTable(constants, glyphInfo, variants);

            return true;
        }

        /// <summary>
        /// Writes all constants, in <see cref="MathConstant"/> order, to <paramref name="output"/>.
        /// </summary>
        /// <remarks>
        /// The percentages and the two UFWORD heights have no device records and are never varied.
        /// </remarks>
        public void ResolveConstants(Span<int> output, ItemVariationStore? store, ReadOnlySpan<float> regionScalers)
        {
            output = output.Slice(0, ConstantCount);

            if (_constants.IsEmpty)
            {
                output.Clear();
                return;
            }

            var span = _constants.Span;

            output[(int)MathConstant.ScriptPercentScaleDown] = BinaryPrimitives.ReadInt16BigEndian(span);
            output[(int)MathConstant.ScriptScriptPercentScaleDown] = BinaryPrimitives.ReadInt16BigEndian(span.Slice(2));
            output[(int)MathConstant.DelimitedSubFormulaMinHeight] = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(4));
            output[(int)MathConstant.DisplayOperatorMinHeight] = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(6));

            for (var i = 0; i < ValueRecordCount; i++)
            {
                output[(int)MathConstant.MathLeading + i] =
                    MathValueRecord.Read(span, FirstValueRecordOffset + i * MathValueRecord.Size, store, regionScalers);
            }

            output[(int)MathConstant.RadicalDegreeBottomRaisePercent] =
                BinaryPrimitives.ReadInt16BigEndian(span.Slice(ConstantsSize - 2));
        }
    }
}
