using System;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.NativeGraphics.Backend
{
    internal class TextShaperStub : ITextShaperImpl
    {
        public GlyphRun ShapeText(ReadOnlySlice<char> text, Typeface typeface, double fontRenderingEmSize, CultureInfo culture)
        {
            return new GlyphRun(new GlyphTypeface(typeface), 1, 
                Enumerable.Repeat((ushort)10, text.Length).ToArray()
                ){Characters = text};
        }
    }
}