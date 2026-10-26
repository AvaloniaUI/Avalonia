namespace Avalonia.Media.Fonts.Tables.Glyf
{
    /// <summary>
    /// Represents a single component of a composite glyph, including its transformation and positioning information.
    /// </summary>
    /// <remarks>A composite glyph is constructed from one or more components, each referencing another glyph
    /// and specifying how it should be transformed and positioned within the composite. This structure encapsulates the
    /// data required to interpret a component according to the OpenType or TrueType font specifications. It is intended
    /// for internal use when parsing or processing composite glyph outlines.</remarks>
    internal readonly struct GlyphComponent
    {
        public CompositeFlags Flags { get; init; }
        public ushort GlyphIndex { get; init; }
        public short Arg1 { get; init; }
        public short Arg2 { get; init; }
        public float Scale { get; init; }
        public float ScaleX { get; init; }
        public float ScaleY { get; init; }
        public float Scale01 { get; init; }
        public float Scale10 { get; init; }
    }
}
