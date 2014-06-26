// -----------------------------------------------------------------------
// <copyright file="ILayoutable.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Layout
{
    public interface ILayoutable
    {
        Size? DesiredSize { get; }

        void Arrange(Rect rect);

        void Measure(Size availableSize);

        void InvalidateArrange();

        void InvalidateMeasure();
    }
}
