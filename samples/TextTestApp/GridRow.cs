using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Layout;

namespace TextTestApp
{
    public class GridRow : Grid
    {
        protected override void ChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            base.ChildrenChanged(sender, e);

            while (Children.Count > ColumnDefinitions.Count)
                ColumnDefinitions.Add(new ColumnDefinition { SharedSizeGroup = "c" + ColumnDefinitions.Count });

            for (int i = 0; i < Children.Count; i++)
            {
                SetColumn(Children[i], i);
                if (Children[i] is Layoutable l)
                    l.VerticalAlignment = VerticalAlignment.Center;
            }
        }
    }
}
