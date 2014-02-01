// -----------------------------------------------------------------------
// <copyright file="TextService.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Windows.Media
{
    using Perspex.Media;
    using SharpDX.DirectWrite;

    public class TextService : ITextService
    {
        private Factory factory;

        public TextService(Factory factory)
        {
            this.factory = factory;
        }

        public static TextFormat Convert(Factory factory, FormattedText text)
        {
            return new TextFormat(
                factory,
                text.FontFamilyName,
                (float)text.FontSize);
        }

        public Size Measure(FormattedText text)
        {
            TextFormat f = Convert(this.factory, text);
            TextLayout layout = new TextLayout(this.factory, text.Text, f, float.MaxValue, float.MaxValue);
            return new Size(
                layout.Metrics.WidthIncludingTrailingWhitespace,
                layout.Metrics.Height);
        }
    }
}
