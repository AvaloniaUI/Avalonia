// -----------------------------------------------------------------------
// <copyright file="IFormattedTextImpl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Platform
{
    using Perspex.Media;

    public interface IFormattedTextImpl
    {
        Size Constraint { get; set; }

        string FontFamilyName { get; set; }

        double FontSize { get; set; }

        FontStyle FontStyle { get; set; }

        string Text { get; set; }

        TextHitTestResult HitTestPoint(Point point);

        Rect HitTestTextPosition(int index);

        Size Measure();
    }
}
