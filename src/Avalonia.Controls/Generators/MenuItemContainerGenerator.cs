namespace Avalonia.Controls.Generators
{
    public class MenuItemContainerGenerator : ItemContainerGenerator<MenuItem>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemContainerGenerator{T}"/> class.
        /// </summary>
        /// <param name="owner">The owner control.</param>
        public MenuItemContainerGenerator(Control owner)
            : base(owner, MenuItem.HeaderProperty, null)
        {
        }

        /// <inheritdoc/>
        protected override Control? CreateContainer(object item)
        {
            var separator = item as Separator;
            return separator != null ? separator : base.CreateContainer(item);
        }
    }
}
