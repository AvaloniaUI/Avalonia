// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

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
    }
}
