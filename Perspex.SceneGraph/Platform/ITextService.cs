// -----------------------------------------------------------------------
// <copyright file="ITextService.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Platform
{
    using System;
    using Perspex.Media;

    [Obsolete("Use methods on FormattedText instead.")]
    public interface ITextService
    {
        int GetCaretIndex(FormattedText text, Point point, Size constraint);

        Point GetCaretPosition(FormattedText text, int caretIndex, Size constraint);

        double[] GetLineHeights(FormattedText text, Size constraint);

        Size Measure(FormattedText text, Size constraint);
    }
}
