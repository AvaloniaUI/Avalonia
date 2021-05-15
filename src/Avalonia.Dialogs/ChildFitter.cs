using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Avalonia.Dialogs
{
    public class ChildFitter : Decorator
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(0, 0);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Child.Measure(finalSize);
            base.ArrangeOverride(finalSize);
            return finalSize;
        }
    }
}
