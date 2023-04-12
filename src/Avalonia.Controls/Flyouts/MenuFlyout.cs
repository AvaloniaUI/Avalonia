using System.Collections;
using System.ComponentModel;
using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using Avalonia.Styling;

namespace Avalonia.Controls
{
    public class MenuFlyout : PopupFlyoutBase
    {
        public MenuFlyout()
        {
            _items = new AvaloniaList<object>();
        }

        /// <summary>
        /// Defines the <see cref="Items"/> property
        /// </summary>
        public static readonly DirectProperty<MenuFlyout, IEnumerable?> ItemsProperty =
            AvaloniaProperty.RegisterDirect<MenuFlyout, IEnumerable?>(
                nameof(Items),
                x => x.Items,
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

        /// <summary>
        /// Defines the <see cref="FlyoutPresenterTheme"/> property.
        /// </summary>
        public static readonly StyledProperty<ControlTheme?> FlyoutPresenterThemeProperty =
            Flyout.FlyoutPresenterThemeProperty.AddOwner<MenuFlyout>();
        
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
            get => GetValue(ItemContainerThemeProperty);
            set => SetValue(ItemContainerThemeProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="ControlTheme"/> that is applied to the container element generated for the flyout presenter.
        /// </summary>
        public ControlTheme? FlyoutPresenterTheme
        {
            get => GetValue(FlyoutPresenterThemeProperty); 
            set => SetValue(FlyoutPresenterThemeProperty, value);
        }
        
        private Classes? _classes;
        private IEnumerable? _items;
        private IDataTemplate? _itemTemplate;

        protected override Control CreatePresenter()
        {
            return new MenuFlyoutPresenter
            {
                [!ItemsControl.ItemsSourceProperty] = this[!ItemsProperty],
                [!ItemsControl.ItemTemplateProperty] = this[!ItemTemplateProperty],
                [!ItemsControl.ItemContainerThemeProperty] = this[!ItemContainerThemeProperty],
            };
        }

        protected override void OnOpening(CancelEventArgs args)
        {
            if (Popup.Child is { } presenter)
            {
                if (_classes != null)
                {
                    SetPresenterClasses(presenter, FlyoutPresenterClasses);
                }

                if (FlyoutPresenterTheme is { } theme)
                {
                    presenter.SetValue(Control.ThemeProperty, theme);
                }
            }

            base.OnOpening(args);
        }
    }
}
