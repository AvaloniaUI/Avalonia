// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Platform;

namespace Avalonia.Cairo.Media.Imaging
{
    using System.IO;
    using Cairo = global::Cairo;

    public class BitmapImpl : IBitmapImpl
    {
        public BitmapImpl(Gdk.Pixbuf pixbuf)
        {
            Surface = pixbuf;
        }

        public int PixelWidth => Surface.Width;

        public int PixelHeight => Surface.Height;

        public Gdk.Pixbuf Surface
        {
            get;
        }

        public void Save(string fileName)
        {
            // TODO: Test
            Surface.Save(fileName, "png");
        }

        public Stream GetStream()
        {
            return new MemoryStream(Surface.SaveToBuffer("png"));
        }
    }
}
