// -----------------------------------------------------------------------
// <copyright file="TextService.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Cairo.Media
{
    using System;
    using System.Linq;
    using Perspex.Media;
    using Perspex.Platform;

    public class TextService : ITextService
    {
        public TextService()
        {
        }

        public int GetCaretIndex(FormattedText text, Point point, Size constraint)
        {
            throw new NotImplementedException();
        }

        public Point GetCaretPosition(FormattedText text, int caretIndex, Size constraint)
        {
            throw new NotImplementedException();
        }

        public double[] GetLineHeights(FormattedText text, Size constraint)
        {
            throw new NotImplementedException();
        }

        public Size Measure(FormattedText text, Size constraint)
        {
            return new Size(100, 30);
        }
    }
}
