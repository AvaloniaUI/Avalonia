// -----------------------------------------------------------------------
// <copyright file="IBitmapImpl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Platform
{
    public interface IBitmapImpl
    {
        int PixelWidth { get; }

        int PixelHeight { get; }

        void Save(string fileName);
    }
}
