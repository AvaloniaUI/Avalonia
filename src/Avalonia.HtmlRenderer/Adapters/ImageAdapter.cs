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

using Avalonia.Media.Imaging;
using TheArtOfDev.HtmlRenderer.Adapters;

namespace TheArtOfDev.HtmlRenderer.Avalonia.Adapters
{
    /// <summary>
    /// Adapter for Avalonia Image object for core.
    /// </summary>
    internal sealed class ImageAdapter : RImage
    {
        /// <summary>
        /// the underline Avalonia image.
        /// </summary>
        private readonly Bitmap _image;

        /// <summary>
        /// Init.
        /// </summary>
        public ImageAdapter(Bitmap image)
        {
            _image = image;
        }

        /// <summary>
        /// the underline Avalonia image.
        /// </summary>
        public Bitmap Image => _image;

        public override double Width => _image.PixelWidth;

        public override double Height => _image.PixelHeight;

        public override void Dispose()
        {
            //TODO: Implement image disposal
            /*if (_image.StreamSource != null)
                _image.StreamSource.Dispose();*/
        }
    }
}