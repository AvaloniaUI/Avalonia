using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Controls.Repeaters
{
    public class NonVirtualizingLayout : Layout
    {
        public override Size Arrange(LayoutContext context, Size finalSize)
        {
            throw new NotImplementedException();
        }

        public override void InitializeForContext(LayoutContext context)
        {
            throw new NotImplementedException();
        }

        public override Size Measure(LayoutContext context, Size availableSize)
        {
            throw new NotImplementedException();
        }

        public override void UninitializeForContext(LayoutContext context)
        {
            throw new NotImplementedException();
        }
    }
}
