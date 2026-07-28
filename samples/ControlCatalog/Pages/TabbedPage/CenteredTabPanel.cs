using System;
using Avalonia;
using Avalonia.Controls;

namespace ControlCatalog.Pages
{
    /// <summary>
    /// A custom panel that arranges N children in N+1 equally-sized slots, leaving the
    /// middle slot empty. Intended for tab bars that need a central action button
    /// overlaid on the gap.
    ///
    /// For N children the split is ⌊N/2⌋ items on the left and ⌈N/2⌉ on the right.
    /// Example – 4 tabs: [0][1][ gap ][2][3]  (5 equal columns, center is free).
    /// </summary>
    public class CenteredTabPanel : Panel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            int count = Children.Count;
            if (count == 0)
                return default;

            int slots = count + 1;
            bool infiniteWidth = double.IsInfinity(availableSize.Width);
            double slotWidth = infiniteWidth ? 60.0 : availableSize.Width / slots;

            double maxHeight = 0;
            foreach (var child in Children)
            {
                child.Measure(new Size(slotWidth, availableSize.Height));
                maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
            }

            // When given finite width, fill it. When infinite (inside a ScrollViewer),
            // return a small positive width so the parent allocates real space.
            double desiredWidth = infiniteWidth ? slotWidth * slots : availableSize.Width;
            if (double.IsNaN(maxHeight) || double.IsInfinity(maxHeight))
                maxHeight = 0;

            return new Size(desiredWidth, maxHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            int count = Children.Count;
            if (count == 0)
                return finalSize;

            int slots     = count + 1;
            double slotW  = finalSize.Width / slots;
            int leftCount = count / 2; // items placed to the left of the gap

            for (int i = 0; i < count; i++)
            {
                // Skip the center slot (leftCount), reserved for the FAB.
                int slot = i < leftCount ? i : i + 1;
                Children[i].Arrange(new Rect(slot * slotW, 0, slotW, finalSize.Height));
            }

            return finalSize;
        }
    }
}
