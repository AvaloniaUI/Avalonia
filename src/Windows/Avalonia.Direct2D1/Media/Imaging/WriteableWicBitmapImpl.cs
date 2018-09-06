// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Platform;
using SharpDX.WIC;
using PixelFormat = Avalonia.Platform.PixelFormat;

namespace Avalonia.Direct2D1.Media.Imaging
{
    class WriteableWicBitmapImpl : WicBitmapImpl, IWriteableBitmapImpl
    {
        public WriteableWicBitmapImpl(int width, int height, PixelFormat? pixelFormat) 
            : base(width, height, pixelFormat)
        {
        }

        class LockedBitmap : ILockedFramebuffer
        {
            private readonly BitmapLock _lock;
            private readonly PixelFormat _format;

            public LockedBitmap(BitmapLock l, PixelFormat format)
            {
                _lock = l;
                _format = format;
            }


            public void Dispose()
            {
                _lock.Dispose();
            }

            public IntPtr Address => _lock.Data.DataPointer;
            public int Width => _lock.Size.Width;
            public int Height => _lock.Size.Height;
            public int RowBytes => _lock.Stride;
            public Vector Dpi { get; } = new Vector(96, 96);
            public PixelFormat Format => _format;

        }

        public ILockedFramebuffer Lock() => new LockedBitmap(WicImpl.Lock(BitmapLockFlags.Write), PixelFormat.Value);
    }
}
