// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Platform;

namespace Avalonia.Cairo.Media.Imaging
{
    using System.IO;
    using Gdk;

    public class BitmapImpl : Pixbuf, IBitmapImpl
    {
        public BitmapImpl(Pixbuf pixbuf)
            :base(pixbuf.SaveToBuffer("png"))
        {
        }

        public int PixelWidth => Width;
        public int PixelHeight => Height;

        public void Save(string fileName)
        {
            // TODO: Test
            Save(fileName, "png");
        }

        public void Save(Stream stream)
        {
            var buffer = SaveToBuffer("png");
            stream.Write(buffer, 0, buffer.Length);
        }
    }
}
