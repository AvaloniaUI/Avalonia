// -----------------------------------------------------------------------
// <copyright file="TextHitTestResult.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    public class TextHitTestResult
    {
        public bool IsInside { get; set; }

        public int TextPosition { get; set; }

        public bool IsTrailing { get; set; }
    }
}
