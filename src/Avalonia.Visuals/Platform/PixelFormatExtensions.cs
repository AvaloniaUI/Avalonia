// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Platform
{
    public static class PixelFormatExtensions
    {
        public static int GetBytesPerPixel(this PixelFormat pixelFormat)
        {
            switch (pixelFormat)
            {
                case PixelFormat.Rgb565:
                    return 2;
                case PixelFormat.Rgba8888:
                case PixelFormat.Bgra8888:
                    return 4;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pixelFormat), pixelFormat, null);
            }
        }
    }
}
