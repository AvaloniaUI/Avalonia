





namespace Perspex.Controls
{
    using Perspex.Controls.Generators;
    using Perspex.Controls.Primitives;

    public class ListBox : SelectingItemsControl
    {
        protected override IItemContainerGenerator CreateItemContainerGenerator()
        {
            return new ItemContainerGenerator<ListBoxItem>(this);
        }
    }
}
