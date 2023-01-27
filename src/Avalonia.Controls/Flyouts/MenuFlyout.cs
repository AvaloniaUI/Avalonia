using System.Collections;
using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using Avalonia.Styling;

namespace Avalonia.Controls
{
    public class MenuFlyout : FlyoutBase
    {
        public MenuFlyout()
        {
            _items = new AvaloniaList<object>();
        }

        /// <summary>
        /// Defines the <see cref="Items"/> property
        /// </summary>
        public static readonly DirectProperty<MenuFlyout, IEnumerable?> ItemsProperty =
            ItemsControl.ItemsProperty.AddOwner<MenuFlyout>(x => x.Items,
                (x, v) => x.Items = v);

        /// <summary>
        /// Defines the <see cref="ItemTemplate"/> property
        /// </summary>
        public static readonly DirectProperty<MenuFlyout, IDataTemplate?> ItemTemplateProperty =
            AvaloniaProperty.RegisterDirect<MenuFlyout, IDataTemplate?>(nameof(ItemTemplate),
                x => x.ItemTemplate, (x, v) => x.ItemTemplate = v);

        /// <summary>
        /// Defines the <see cref="ItemContainerTheme"/> property.
        /// </summary>
        public static readonly StyledProperty<ControlTheme?> ItemContainerThemeProperty =
            ItemsControl.ItemContainerThemeProperty.AddOwner<MenuFlyout>();

        public Classes FlyoutPresenterClasses => _classes ??= new Classes();

        /// <summary>
        /// Gets or sets the items of the MenuFlyout
        /// </summary>
        [Content]
        public IEnumerable? Items
        {
            get => _items;
            set => SetAndRaise(ItemsProperty, ref _items, value);
        }

        /// <summary>
        /// Gets or sets the template used for the items
        /// </summary>
        public IDataTemplate? ItemTemplate
        {
            get => _itemTemplate;
            set => SetAndRaise(ItemTemplateProperty, ref _itemTemplate, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="ControlTheme"/> that is applied to the container element generated for each item.
        /// </summary>
        public ControlTheme? ItemContainerTheme
        {
            get { return GetValue(ItemContainerThemeProperty); }
            set { SetValue(ItemContainerThemeProperty, value); }
        }

        private Classes? _classes;
        private IEnumerable? _items;
        private IDataTemplate? _itemTemplate;

        protected override Control CreatePresenter()
        {
            return new MenuFlyoutPresenter
            {
                [!ItemsControl.ItemsProperty] = this[!ItemsProperty],
                [!ItemsControl.ItemTemplateProperty] = this[!ItemTemplateProperty],
                [!ItemsControl.ItemContainerThemeProperty] = this[!ItemContainerThemeProperty],
            };
        }

        protected override void OnOpened()
        {
            if (_classes != null)
            {
                SetPresenterClasses(Popup.Child, FlyoutPresenterClasses);
            }
            base.OnOpened();
        }
    }
}
