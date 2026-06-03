using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Media.Fonts.Tables.Name;

namespace Avalonia.Media.Fonts.Tables.Variation
{
    /// <summary>
    /// Parses the OpenType 'fvar' (font variations) table. Provides the variation axis
    /// definitions and named instances declared by a variable font.
    /// </summary>
    /// <remarks>
    /// <para>
    /// fvar declares which design axes a variable font exposes (weight, width, optical size,
    /// slant, italic, custom axes) and any pre-named instances ("Regular", "Bold", etc.) the
    /// designer has curated. It does NOT carry the per-glyph deltas — those live in 'gvar'
    /// (outlines), HVAR / VVAR (advances and side bearings), and MVAR (font-wide metrics).
    /// </para>
    /// <para>
    /// Reference: <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar"/>.
    /// </para>
    /// </remarks>
    internal sealed class FvarTable
    {
        internal const string TableName = "fvar";
        internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse(TableName);

        // Flag bits inside VariationAxisRecord.flags. Only bit 0 is currently defined.
        private const ushort HiddenAxisFlag = 0x0001;

        // The fvar table version we understand. The spec has only ever defined v1.0.
        private const ushort SupportedMajorVersion = 1;

        // Per the spec, axisSize is always 20 in v1.0. Implementations are required to be
        // forward-compatible with a larger axisSize, so we treat 20 as a lower bound and
        // seek past any extra trailing bytes when reading each axis record.
        private const ushort MinimumAxisSize = 20;

        private FvarTable(
            ImmutableArray<FontVariationAxis> axes,
            ImmutableArray<FontVariationInstance> instances,
            ImmutableArray<OpenTypeTag> axisTags)
        {
            Axes = axes;
            Instances = instances;
            AxisTags = axisTags;
        }

        /// <summary>
        /// Gets the variation axes declared by the font, in the order they appear in the
        /// table. Indexable; the order matches the per-instance coordinate order.
        /// </summary>
        public ImmutableArray<FontVariationAxis> Axes { get; }

        /// <summary>
        /// Gets the named variation instances declared by the font, in declaration order.
        /// </summary>
        public ImmutableArray<FontVariationInstance> Instances { get; }

        /// <summary>
        /// Gets the axis tags in declaration order. Kept as a separate array so consumers
        /// reading raw coordinate sequences (e.g. avar segment maps) can index into the
        /// right axis without paying the cost of unpacking the public
        /// <see cref="FontVariationAxis"/> record.
        /// </summary>
        public ImmutableArray<OpenTypeTag> AxisTags { get; }

        /// <summary>
        /// Loads the fvar table from <paramref name="glyphTypeface"/>, resolving axis and
        /// instance names via <paramref name="nameTable"/> when available.
        /// </summary>
        /// <returns>
        /// <c>true</c> when the font carries an fvar table that we recognize and could
        /// parse; <c>false</c> for static fonts, for an unrecognized version, or for a
        /// malformed table.
        /// </returns>
        public static bool TryLoad(
            GlyphTypeface glyphTypeface,
            NameTable? nameTable,
            [NotNullWhen(true)] out FvarTable? fvarTable)
        {
            fvarTable = null;

            if (!glyphTypeface.PlatformTypeface.TryGetTable(Tag, out var data))
            {
                return false;
            }

            var span = data.Span;
            if (span.Length < 16)
            {
                return false;
            }

            var reader = new BigEndianBinaryReader(span);

            // Header (16 bytes).
            var majorVersion = reader.ReadUInt16();
            _ = reader.ReadUInt16(); // minorVersion — ignored; v1.x is forward-compatible.

            if (majorVersion != SupportedMajorVersion)
            {
                return false;
            }

            var axesArrayOffset = reader.ReadOffset16();
            _ = reader.ReadUInt16(); // Reserved field, set to 2 per spec.
            var axisCount = reader.ReadUInt16();
            var axisSize = reader.ReadUInt16();
            var instanceCount = reader.ReadUInt16();
            var instanceSize = reader.ReadUInt16();

            if (axisCount == 0 || axisSize < MinimumAxisSize)
            {
                return false;
            }

            // Reject the table when the declared instance layout cannot even hold the
            // mandatory (subfamilyNameID, flags, coordinates) prefix — a strong signal
            // that the table is malformed and not worth parsing further.
            var minimumInstanceSize = 4 + axisCount * 4;
            if (instanceCount != 0 && instanceSize < minimumInstanceSize)
            {
                return false;
            }

            var axesEnd = axesArrayOffset + axisCount * axisSize;
            var instancesEnd = axesEnd + instanceCount * instanceSize;
            if (instancesEnd > span.Length)
            {
                return false;
            }

            // English (US) — matches GlyphTypeface's existing FamilyName / TypographicFamilyName
            // resolution. Localization is deferred to a follow-up; see PR4 planning doc.
            var culture = (ushort)CultureInfo.InvariantCulture.LCID;

            var axisTagsBuilder = ImmutableArray.CreateBuilder<OpenTypeTag>(axisCount);
            var axesBuilder = ImmutableArray.CreateBuilder<FontVariationAxis>(axisCount);

            for (var i = 0; i < axisCount; i++)
            {
                reader.Seek(axesArrayOffset + i * axisSize);

                var tag = new OpenTypeTag(reader.ReadUInt32());
                var minValue = reader.ReadFixed();
                var defaultValue = reader.ReadFixed();
                var maxValue = reader.ReadFixed();
                var flags = reader.ReadUInt16();
                var axisNameId = reader.ReadUInt16();

                var name = nameTable?.GetNameById(culture, axisNameId);
                if (string.IsNullOrEmpty(name))
                {
                    name = tag.ToString();
                }

                axisTagsBuilder.Add(tag);
                axesBuilder.Add(new FontVariationAxis(
                    Tag: tag,
                    Name: name,
                    MinimumValue: minValue,
                    DefaultValue: defaultValue,
                    MaximumValue: maxValue,
                    IsHidden: (flags & HiddenAxisFlag) != 0));
            }

            // InstanceRecord.postScriptNameID is optional. The spec encodes "present" via
            // instanceSize being big enough to hold the extra uint16 after the coordinates.
            var hasPostScriptName = instanceSize >= minimumInstanceSize + 2;

            var instancesBuilder = ImmutableArray.CreateBuilder<FontVariationInstance>(instanceCount);

            for (var i = 0; i < instanceCount; i++)
            {
                reader.Seek(axesEnd + i * instanceSize);

                var subfamilyNameId = reader.ReadUInt16();
                _ = reader.ReadUInt16(); // flags — reserved, must be 0.

                var coordinates = new Dictionary<OpenTypeTag, float>(axisCount);
                for (var a = 0; a < axisCount; a++)
                {
                    coordinates[axisTagsBuilder[a]] = reader.ReadFixed();
                }

                int? postScriptNameId = null;
                if (hasPostScriptName)
                {
                    postScriptNameId = reader.ReadUInt16();
                }

                var instanceName = nameTable?.GetNameById(culture, subfamilyNameId) ?? string.Empty;

                instancesBuilder.Add(new FontVariationInstance(
                    instanceName,
                    i,
                    coordinates,
                    postScriptNameId));
            }

            fvarTable = new FvarTable(
                axesBuilder.MoveToImmutable(),
                instancesBuilder.MoveToImmutable(),
                axisTagsBuilder.MoveToImmutable());

            return true;
        }
    }
}
