// -----------------------------------------------------------------------
// <copyright file="FormattedText.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    public enum FontStyle
    {
        Normal,
        Oblique,
        Italic,
    }

    public class FormattedText
    {
        public string FontFamilyName { get; set; }

        public double FontSize { get; set; }

        public FontStyle FontStyle { get; set; }

        public string Text { get; set; }
    }
}
