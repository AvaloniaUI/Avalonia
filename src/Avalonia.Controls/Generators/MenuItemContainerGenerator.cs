#nullable enable

using Avalonia.Data;

namespace Avalonia.Controls.Generators
{
    public class MenuItemContainerGenerator : IItemContainerGenerator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemContainerGenerator{T}"/> class.
        /// </summary>
        /// <param name="owner">The owner control.</param>
        public MenuItemContainerGenerator(ItemsControl owner)
        {
            Owner = owner;
        }

        public ItemsControl Owner { get; }

        public IControl Realize(IControl parent, int index, object? item)
        {
            if (item is Separator separator)
                return separator;
            if (item is MenuItem menuItem)
                return menuItem;

            menuItem = new MenuItem
            {
                DataContext = item,
            };

            menuItem.Bind(
                MenuItem.HeaderProperty,
                menuItem.GetBindingObservable(StyledElement.DataContextProperty),
                BindingPriority.TemplatedParent);

            return menuItem;
        }

        public void Unrealize(IControl container, int index, object? item)
        {
            throw new System.NotImplementedException();
        }
    }
}
