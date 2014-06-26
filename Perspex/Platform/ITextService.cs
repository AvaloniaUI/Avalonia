// -----------------------------------------------------------------------
// <copyright file="ITextService.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Platform
{
    using Perspex.Media;

    public interface ITextService
    {
        Size Measure(FormattedText text);

        int GetCaretIndex(FormattedText text, Point point);

        Point GetCaretPosition(FormattedText text, int caretIndex);
    }
}
