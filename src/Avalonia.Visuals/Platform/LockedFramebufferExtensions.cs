// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Media;

namespace Avalonia.Platform
{
    public static class LockedFramebufferExtensions
    {
        public static Span<byte> GetPixels(this ILockedFramebuffer framebuffer)
        {
            unsafe
            {
                return new Span<byte>((byte*)framebuffer.Address, framebuffer.RowBytes * framebuffer.Size.Height);
            }
        }

        public static Span<byte> GetPixel(this ILockedFramebuffer framebuffer, int x, int y)
        {
            unsafe
            {
                var bytesPerPixel = framebuffer.Format.GetBytesPerPixel();
                var zero = (byte*)framebuffer.Address;
                var offset = framebuffer.RowBytes * y + bytesPerPixel * x;
                return new Span<byte>(zero + offset, bytesPerPixel);
            }
        }

        public static void SetPixel(this ILockedFramebuffer framebuffer, int x, int y, Color color)
        {
            var pixel = framebuffer.GetPixel(x, y);

            var alpha = color.A / 255.0;

            switch (framebuffer.Format)
            {
                case PixelFormat.Rgb565:
                    pixel[0] = color.R;
                    pixel[1] = color.G;
                    pixel[2] = color.B;
                    break;

                case PixelFormat.Rgba8888:
                    pixel[0] = (byte)((color.R - (1 - alpha) * 255) / alpha);
                    pixel[1] = (byte)((color.G - (1 - alpha) * 255) / alpha);
                    pixel[2] = (byte)((color.B - (1 - alpha) * 255) / alpha);
                    pixel[3] = color.A;
                    break;

                case PixelFormat.Bgra8888:
                    pixel[0] = (byte)((color.B - (1 - alpha) * 255) / alpha);
                    pixel[1] = (byte)((color.G - (1 - alpha) * 255) / alpha);
                    pixel[2] = (byte)((color.R - (1 - alpha) * 255) / alpha);
                    pixel[3] = color.A;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
