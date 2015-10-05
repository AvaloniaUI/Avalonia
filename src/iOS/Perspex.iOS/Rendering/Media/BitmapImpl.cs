using System;
using Perspex.Platform;
using UIKit;
using Foundation;
using CoreGraphics;

namespace Perspex.iOS.Rendering.Media
{
    public class BitmapImpl : IBitmapImpl
    {
        UIImage _image;
        public CGImage Image { get { return _image.CGImage; } }

        public BitmapImpl(string filename)
        {
            // not sure the best way to load images in a platform compatible way.
            // for now iOS requires that the image be placed in the iOS.exe's Resources folder
            // so this call to FromBundle succeeds
            //
            _image = UIImage.FromBundle(filename);
            //_image = LoadImageFromUrl(filename);
        }

        /// <summary>
        /// Gets the width of the bitmap, in pixels.
        /// </summary>
        public int PixelWidth => (int)_image.Size.Width;

        /// <summary>
        /// Gets the height of the bitmap, in pixels.
        /// </summary>
        public int PixelHeight => (int)_image.Size.Height;


        public void Save(string fileName)
        {
            throw new NotImplementedException();
        }

        static UIImage LoadImageFromUrl(string uri)
        {
            using (var url = new NSUrl(uri))
            using (var data = NSData.FromUrl(url))
            {
                return UIImage.LoadFromData(data);
            }
        }
    }
}
