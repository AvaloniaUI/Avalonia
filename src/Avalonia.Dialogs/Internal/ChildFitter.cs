using Avalonia.Controls;

namespace Avalonia.Dialogs.Internal
{
    public class ChildFitter : Decorator
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(0, 0);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Child?.Measure(finalSize);
            base.ArrangeOverride(finalSize);
            return finalSize;
        }
    }
}
