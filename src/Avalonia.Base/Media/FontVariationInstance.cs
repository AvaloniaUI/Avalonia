using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Avalonia.Media.Fonts;

namespace Avalonia.Media
{
    /// <summary>
    /// Describes a named variation instance declared by a variable font's <c>fvar</c> table.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Named instances are pre-defined points in variation space the font designer has
    /// labeled — for example "Regular" at <c>wght=400</c>, or "Light Condensed" at
    /// <c>wght=300, wdth=75</c>. They give applications a curated set of preset combinations
    /// without having to invent their own.
    /// </para>
    /// <para>
    /// Coordinates are in <b>user-space</b> (the same values the font designer chose to
    /// expose); pass them through <c>GlyphTypeface.CreateVariationSettings</c> — either as a
    /// dictionary or via <c>CreateVariationSettings(instanceIndex: instance.Index)</c> — to
    /// produce the normalized <see cref="FontVariationSettings"/> required by the rendering
    /// pipeline.
    /// </para>
    /// </remarks>
    public readonly struct FontVariationInstance : IEquatable<FontVariationInstance>
    {
        private readonly IReadOnlyDictionary<OpenTypeTag, float>? _coordinates;

        internal FontVariationInstance(
            string name,
            int index,
            IReadOnlyDictionary<OpenTypeTag, float> coordinates,
            int? postScriptNameId)
        {
            Name = name ?? string.Empty;
            Index = index;
            _coordinates = coordinates;
            PostScriptNameId = postScriptNameId;
        }

        /// <summary>
        /// Gets the human-readable instance name resolved from the font's <c>name</c> table
        /// (e.g. "Regular", "SemiBold", "Light Condensed"). Empty when the font omits the
        /// name record for this instance.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the zero-based position of this instance in the font's <c>fvar</c> instance
        /// array. Use this with the <c>instanceIndex</c> parameter on
        /// <c>GlyphTypeface.CreateVariationSettings</c> as a shorthand for "give me a
        /// settings value for this preset".
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Gets the user-space axis coordinates that define this instance, keyed by axis tag
        /// (e.g. <c>{ wght: 700, wdth: 100 }</c> for a Bold). Always non-null; empty when the
        /// instance has no axis values declared (a malformed font).
        /// </summary>
        public IReadOnlyDictionary<OpenTypeTag, float> Coordinates
            => _coordinates ?? (IReadOnlyDictionary<OpenTypeTag, float>)ImmutableDictionary<OpenTypeTag, float>.Empty;

        /// <summary>
        /// Gets the optional name-ID of a PostScript name record for this instance, when the
        /// font declares one. <c>null</c> when not present.
        /// </summary>
        public int? PostScriptNameId { get; }

        /// <summary>
        /// Two instances are equal when they describe the same preset on the same font —
        /// i.e. they have the same name, index, and PostScript name ID. Coordinates are
        /// not compared because the dictionary type doesn't support value equality;
        /// within a font, <see cref="Index"/> already uniquely identifies an instance.
        /// </summary>
        public bool Equals(FontVariationInstance other)
            => Index == other.Index
            && PostScriptNameId == other.PostScriptNameId
            && string.Equals(Name, other.Name, StringComparison.Ordinal);

        public override bool Equals(object? obj)
            => obj is FontVariationInstance other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(Name, Index, PostScriptNameId);

        public static bool operator ==(FontVariationInstance left, FontVariationInstance right)
            => left.Equals(right);

        public static bool operator !=(FontVariationInstance left, FontVariationInstance right)
            => !left.Equals(right);
    }
}
