// "Therefore those skilled at the unorthodox
// are infinite as heaven and earth,
// inexhaustible as the great rivers.
// When they come to an end,
// they begin again,
// like the days and months;
// they die and are reborn,
// like the four seasons."
// 
// - Sun Tsu,
// "The Art of War"

using Avalonia.Media;
using TheArtOfDev.HtmlRenderer.Adapters;
using TheArtOfDev.HtmlRenderer.Adapters.Entities;

namespace TheArtOfDev.HtmlRenderer.Avalonia.Adapters
{
    /// <summary>
    /// Adapter for Avalonia Font.
    /// </summary>
    internal sealed class FontAdapter : RFont
    {
        public RFontStyle Style { get; }

        #region Fields and Consts


        /// <summary>
        /// the size of the font
        /// </summary>
        private readonly double _size;

        /// <summary>
        /// the vertical offset of the font underline location from the top of the font.
        /// </summary>
        private readonly double _underlineOffset = -1;

        /// <summary>
        /// Cached font height.
        /// </summary>
        private readonly double _height = -1;

        /// <summary>
        /// Cached font whitespace width.
        /// </summary>
        private double _whitespaceWidth = -1;
        

        #endregion


        /// <summary>
        /// Init.
        /// </summary>
        public FontAdapter(string fontFamily, double size, RFontStyle style)
        {
            Style = style;
            Name = fontFamily;
            _size = size;
            //TODO: Somehow get proper line spacing and underlinePosition
            var lineSpacing = 1;
            var underlinePosition = 0;

            _height = 96d / 72d * _size * lineSpacing;
            _underlineOffset = 96d / 72d * _size * (lineSpacing + underlinePosition);

        }

        public string Name { get; set; }


        public override double Size
        {
            get { return _size; }
        }

        public override double UnderlineOffset
        {
            get { return _underlineOffset; }
        }

        public override double Height
        {
            get { return _height; }
        }

        public override double LeftPadding
        {
            get { return _height / 6f; }
        }

        public override double GetWhitespaceWidth(RGraphics graphics)
        {
            if (_whitespaceWidth < 0)
            {
                _whitespaceWidth = graphics.MeasureString(" ", this).Width;
            }
            return _whitespaceWidth;
        }

        public FontStyle FontStyle => Style.HasFlag(RFontStyle.Italic) ? FontStyle.Italic : FontStyle.Normal;

        public FontWeight Weight => Style.HasFlag(RFontStyle.Bold) ? FontWeight.Bold : FontWeight.Normal;
    }
}