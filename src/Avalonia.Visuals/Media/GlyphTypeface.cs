// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

using Avalonia.Platform;

namespace Avalonia.Media
{
    public sealed class GlyphTypeface : IDisposable
    {
        private static readonly IPlatformRenderInterface s_platformRenderInterface =
            AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();

        public GlyphTypeface(Typeface typeface) : this(s_platformRenderInterface.CreateGlyphTypeface(typeface))
        {
        }

        public GlyphTypeface(IGlyphTypefaceImpl platformImpl)
        {
            PlatformImpl = platformImpl;
        }

        public IGlyphTypefaceImpl PlatformImpl { get; }

        /// <summary>
        ///     Gets the font design units per em.
        /// </summary>
        public short DesignEmHeight => PlatformImpl.DesignEmHeight;

        /// <summary>
        ///     Gets the recommended distance above the baseline in design em size. 
        /// </summary>
        public int Ascent => PlatformImpl.Ascent;

        /// <summary>
        ///     Gets the recommended distance under the baseline in design em size. 
        /// </summary>
        public int Descent => PlatformImpl.Descent;

        /// <summary>
        ///      Gets the recommended additional space between two lines of text in design em size. 
        /// </summary>
        public int LineGap => PlatformImpl.LineGap;

        /// <summary>
        ///     Gets the recommended line height.
        /// </summary>
        public int LineHeight => Descent - Ascent + LineGap;

        /// <summary>
        ///     Gets a value that indicates the distance of the underline from the baseline in design em size.
        /// </summary>
        public int UnderlinePosition => PlatformImpl.UnderlinePosition;

        /// <summary>
        ///     Gets a value that indicates the thickness of the underline in design em size.
        /// </summary>
        public int UnderlineThickness => PlatformImpl.UnderlineThickness;

        /// <summary>
        ///     Gets a value that indicates the distance of the strikethrough from the baseline in design em size.
        /// </summary>
        public int StrikethroughPosition => PlatformImpl.StrikethroughPosition;

        /// <summary>
        ///     Gets a value that indicates the thickness of the underline in design em size.
        /// </summary>
        public int StrikethroughThickness => PlatformImpl.StrikethroughThickness;

        /// <summary>
        ///     Returns an glyph index for the specified codepoint.
        /// </summary>
        /// <remarks>
        ///     Returns <c>0</c> if a glyph isn't found.
        /// </remarks>
        /// <param name="codepoint">The codepoint.</param>
        /// <returns>
        ///     A glyph index.
        /// </returns>
        public ushort GetGlyph(uint codepoint) => PlatformImpl.GetGlyph(codepoint);

        /// <summary>
        ///     Returns an array of glyph indices. Codepoints that are not represented by the font are returned as <code>0</code>.
        /// </summary>
        /// <param name="codepoints">The codepoints to map.</param>
        /// <returns></returns>
        public ushort[] GetGlyphs(ReadOnlySpan<uint> codepoints) => PlatformImpl.GetGlyphs(codepoints);

        /// <summary>
        ///     Returns the glyph advance for the specified glyph.
        /// </summary>
        /// <param name="glyph">The glyph.</param>
        /// <returns>
        ///     The advance.
        /// </returns>
        public int GetGlyphAdvance(ushort glyph) => PlatformImpl.GetGlyphAdvance(glyph);

        /// <summary>
        ///     Returns an array of glyph advances in design em size.
        /// </summary>
        /// <param name="glyphs">The glyph indices.</param>
        /// <returns></returns>
        public int[] GetGlyphAdvances(ReadOnlySpan<ushort> glyphs) => PlatformImpl.GetGlyphAdvances(glyphs);

        void IDisposable.Dispose()
        {
            PlatformImpl?.Dispose();
        }
    }
}
