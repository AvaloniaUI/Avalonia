using System;

namespace Avalonia.Media
{
    /// <summary>
    /// The parts from which a math glyph is assembled when no size variant is large enough.
    /// </summary>
    /// <param name="ItalicsCorrection">
    /// The italics correction of the assembled glyph, in design units, at the typeface's variation
    /// instance.
    /// </param>
    /// <param name="Parts">
    /// The parts in the font's order: bottom to top for a vertical assembly, left to right for a
    /// horizontal one. A vertical assembly is therefore listed in the opposite order of Avalonia's
    /// downward Y axis.
    /// </param>
    public readonly record struct MathGlyphAssembly(int ItalicsCorrection, ReadOnlyMemory<MathGlyphPart> Parts);
}
