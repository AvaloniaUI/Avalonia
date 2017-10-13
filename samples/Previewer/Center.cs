using Avalonia;
using Avalonia.Controls;

namespace Previewer
{
    public class Center : Decorator
    {
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Child != null)
            {
                var desired = Child.DesiredSize;
                Child.Arrange(new Rect((finalSize.Width - desired.Width) / 2, (finalSize.Height - desired.Height) / 2,
                    desired.Width, desired.Height));
            }
            return finalSize;
        }
    }
}