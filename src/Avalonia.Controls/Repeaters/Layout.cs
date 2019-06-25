// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;

namespace Avalonia.Controls.Repeaters
{
    public abstract class Layout : AvaloniaObject
    {
        public string LayoutId { get; set; }

        public event EventHandler MeasureInvalidated;
        public event EventHandler ArrangeInvalidated;

        public abstract void InitializeForContext(LayoutContext context);

        public abstract void UninitializeForContext(LayoutContext context);

        public abstract Size Measure(LayoutContext context, Size availableSize);

        public abstract Size Arrange(LayoutContext context, Size finalSize);

        protected void InvalidateMeasure() => MeasureInvalidated?.Invoke(this, EventArgs.Empty);

        protected void InvalidateArrange() => ArrangeInvalidated?.Invoke(this, EventArgs.Empty);
    }
}
