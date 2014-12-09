// -----------------------------------------------------------------------
// <copyright file="IFormattedTextImpl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Platform
{
    using System;
    using System.Collections.Generic;
    using Perspex.Media;

    public interface IFormattedTextImpl : IDisposable
    {
        Size Constraint { get; set; }

        IEnumerable<FormattedTextLine> GetLines();

        TextHitTestResult HitTestPoint(Point point);

        Rect HitTestTextPosition(int index);

        IEnumerable<Rect> HitTestTextRange(int index, int length, Point origin);

        Size Measure();
    }
}
