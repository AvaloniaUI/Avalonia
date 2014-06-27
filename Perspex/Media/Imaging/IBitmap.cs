// -----------------------------------------------------------------------
// <copyright file="IBitmap.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media.Imaging
{
    public interface IBitmap
    {
        int PixelHeight { get; }

        int PixelWidth { get; }

        void Save(string fileName);
    }
}