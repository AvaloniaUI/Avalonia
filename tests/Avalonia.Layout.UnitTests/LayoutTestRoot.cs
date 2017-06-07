// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.UnitTests;

namespace Avalonia.Layout.UnitTests
{
    internal class LayoutTestRoot : TestRoot, ILayoutable
    {
        public bool Measured { get; set; }
        public bool Arranged { get; set; }
        public Func<ILayoutable, Size, Size> DoMeasureOverride { get; set; }
        public Func<ILayoutable, Size, Size> DoArrangeOverride { get; set; }

        void ILayoutable.Measure(Size availableSize)
        {
            Measured = true;
            Measure(availableSize);
        }

        void ILayoutable.Arrange(Rect rect)
        {
            Arranged = true;
            Arrange(rect);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return DoMeasureOverride != null ?
                DoMeasureOverride(this, availableSize) :
                base.MeasureOverride(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Arranged = true;
            return DoArrangeOverride != null ?
                DoArrangeOverride(this, finalSize) :
                base.ArrangeOverride(finalSize);
        }
    }
}
