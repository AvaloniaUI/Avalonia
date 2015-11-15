// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Platform;
using System.IO;
using AG = Android.Graphics;

namespace Perspex.Android.CanvasRendering
{
    public class BitmapImpl : IBitmapImpl
    {
        public BitmapImpl(AG.Bitmap bitmap)
        {
            PlatformBitmap = bitmap;
        }

        public int PixelWidth => PlatformBitmap.Width;

        public int PixelHeight => PlatformBitmap.Height;

        public AG.Bitmap PlatformBitmap
        {
            get;
        }

        public void Save(string fileName)
        {
            using (var f = File.OpenWrite(fileName))
            {
                PlatformBitmap.Compress(AG.Bitmap.CompressFormat.Png, 100, f);
            }
        }
    }
}