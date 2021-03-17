namespace Avalonia.Controls
{
    using System.Collections;

    using Avalonia.Collections;
    using Avalonia.Controls.Primitives;
    using Avalonia.Metadata;

    public class MenuFlyout : FlyoutBase
    {
        public static readonly DirectProperty<MenuFlyout, IEnumerable> ItemsProperty =
            ItemsControl.ItemsProperty.AddOwner<MenuFlyout>(o => o.Items);

        private IEnumerable _items;

        public MenuFlyout()
        {
            _items = new AvaloniaList<TemplatedControl>();
        }

        /// <summary>
        /// Gets or sets the items to display.
        /// </summary>
        [Content]
        public IEnumerable Items
        {
            get => _items;
            private set => SetAndRaise(ItemsProperty, ref _items, value);
        }

        protected override Control CreatePresenter()
        {
            return new MenuFlyoutPresenter(this)
            {
                [!ItemsProperty] = this[!ItemsProperty]
            };
        }
    }
}
