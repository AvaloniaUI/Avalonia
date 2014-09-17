// -----------------------------------------------------------------------
// <copyright file="ILayoutable.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Layout
{
    public interface ILayoutable : IVisual
    {
        Size? DesiredSize { get; }

        double Width { get;  }

        double Height { get;  }

        double MinWidth { get; }

        double MaxWidth { get; }

        double MinHeight { get; }

        double MaxHeight { get; }

        HorizontalAlignment HorizontalAlignment { get; }

        VerticalAlignment VerticalAlignment { get; }

        void Arrange(Rect rect);

        void Measure(Size availableSize);

        void InvalidateArrange();

        void InvalidateMeasure();
    }
}
