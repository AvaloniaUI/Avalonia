// -----------------------------------------------------------------------
// <copyright file="IBitmap.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media.Imaging
{
    public interface IBitmap
    {
        int PixelWidth { get; }

        int PixelHeight { get; }

        void Save(string fileName);
    }
}
