using Avalonia.Media.Fonts;

namespace Avalonia.Media
{
    /// <summary>
    /// Describes a single variation axis declared by a variable font's <c>fvar</c> table.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Variation axes are the design dimensions a variable font exposes — typical examples
    /// include <c>wght</c> (weight, 1..1000), <c>wdth</c> (width, percentage), <c>opsz</c>
    /// (optical size, points), <c>ital</c> (italic, 0..1), and <c>slnt</c> (slant, degrees).
    /// </para>
    /// <para>
    /// Values are in <b>user-space</b>: the same values the font designer chose to expose to
    /// end users (e.g. weight <c>700</c> for "Bold"). The platform-neutral
    /// <see cref="FontVariationSettings"/> stores <i>normalized</i> values in <c>[-1, 1]</c>
    /// after the font's <c>fvar</c> / <c>avar</c> tables have been applied; use
    /// <c>GlyphTypeface.CreateVariationSettings</c> to convert from user-space to normalized.
    /// </para>
    /// </remarks>
    /// <param name="Tag">The four-character OpenType axis tag (e.g. <c>wght</c>).</param>
    /// <param name="Name">
    /// A human-readable axis name resolved from the font's <c>name</c> table. Falls back to the
    /// tag's string form when no name record is available.
    /// </param>
    /// <param name="MinimumValue">The minimum allowed user-space value for this axis.</param>
    /// <param name="DefaultValue">The default user-space value (what <c>default(FontVariationSettings)</c> selects).</param>
    /// <param name="MaximumValue">The maximum allowed user-space value for this axis.</param>
    /// <param name="IsHidden">
    /// <c>true</c> if the font designer flagged this axis as hidden (intended for internal use
    /// rather than direct user exposure). Hidden axes are typically still functional but should
    /// not be surfaced in a font-picker UI.
    /// </param>
    public readonly record struct FontVariationAxis(
        OpenTypeTag Tag,
        string Name,
        float MinimumValue,
        float DefaultValue,
        float MaximumValue,
        bool IsHidden);
}
