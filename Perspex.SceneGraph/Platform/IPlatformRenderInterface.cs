// -----------------------------------------------------------------------
// <copyright file="IPlatformRenderInterface.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Platform
{
    using Perspex.Media;

    public interface IPlatformRenderInterface
    {
        IBitmapImpl CreateBitmap(int width, int height);

        IFormattedTextImpl CreateFormattedText(
            string text, 
            string fontFamily, 
            double fontSize, 
            FontStyle fontStyle,
            TextAlignment textAlignment,
            FontWeight fontWeight);

        IStreamGeometryImpl CreateStreamGeometry();

        IRenderer CreateRenderer(IPlatformHandle handle, double width, double height);

        IRenderTargetBitmapImpl CreateRenderTargetBitmap(int width, int height);

        IBitmapImpl LoadBitmap(string fileName);
    }
}
