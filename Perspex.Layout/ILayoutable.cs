// -----------------------------------------------------------------------
// <copyright file="ILayoutable.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Layout
{
    // TODO: Probably want to move width/height/etc properties to different interface.
    public interface ILayoutable : IVisual
    {
        Size DesiredSize { get; }

        double Width { get;  }

        double Height { get;  }

        double MinWidth { get; }

        double MaxWidth { get; }

        double MinHeight { get; }

        double MaxHeight { get; }

        HorizontalAlignment HorizontalAlignment { get; }

        VerticalAlignment VerticalAlignment { get; }

        bool IsMeasureValid { get; }

        bool IsArrangeValid { get; }

        Size? PreviousMeasure { get; }

        Rect? PreviousArrange { get; }

        void ApplyTemplate();

        void Measure(Size availableSize, bool force = false);

        void Arrange(Rect rect, bool force = false);

        void InvalidateMeasure();

        void InvalidateArrange();
    }
}
