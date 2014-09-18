// -----------------------------------------------------------------------
// <copyright file="IBitmap.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media.Imaging
{
    using Perspex.Platform;

    public interface IBitmap
    {
        int PixelWidth { get; }

        int PixelHeight { get; }

        IBitmapImpl PlatformImpl { get; }

        void Save(string fileName);
    }
}
