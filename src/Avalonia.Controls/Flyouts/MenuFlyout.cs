using System.Collections;
using System.Collections.Specialized;
using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Metadata;
using Avalonia.Styling;

#nullable enable

namespace Avalonia.Controls
{
    public class MenuFlyout : FlyoutBase
    {
        public MenuFlyout()
        {
            _items = new AvaloniaList<object>();
        }

        public static readonly DirectProperty<MenuFlyout, IEnumerable> ItemsProperty =
            ItemsControl.ItemsProperty.AddOwner<MenuFlyout>(x => x.Items,
                (x, v) => x.Items = v);

        public static readonly DirectProperty<MenuFlyout, IDataTemplate?> ItemTemplateProperty =
            AvaloniaProperty.RegisterDirect<MenuFlyout, IDataTemplate?>(nameof(ItemTemplate),
                x => x.ItemTemplate, (x, v) => x.ItemTemplate = v);

        public Classes? FlyoutPresenterClasses => _classes ??= new Classes();

        [Content]
        public IEnumerable Items
        {
            get => _items;
            set => SetAndRaise(ItemsProperty, ref _items, value);
        }

        public IDataTemplate? ItemTemplate
        {
            get => _itemTemplate;
            set => SetAndRaise(ItemTemplateProperty, ref _itemTemplate, value);
        }

        private Classes? _classes;
        private IEnumerable _items;
        private IDataTemplate? _itemTemplate;

        protected override Control CreatePresenter()
        {
            return new MenuFlyoutPresenter
            {
                [!ItemsControl.ItemsProperty] = this[!ItemsProperty],
                [!ItemsControl.ItemTemplateProperty] = this[!ItemTemplateProperty]
            };
        }

        protected override void OnOpened()
        {
            if (FlyoutPresenterClasses != null)
            {
                //Remove any classes no longer in use
                for (int i = _popup.Child.Classes.Count - 1; i >= 0; i--)
                {
                    if (!FlyoutPresenterClasses.Contains(_popup.Child.Classes[i]))
                    {
                        _popup.Child.Classes.RemoveAt(i);
                    }
                }

                //Add new classes
                _popup.Child.Classes.AddRange(FlyoutPresenterClasses);
            }
            base.OnOpened();
        }
    }
}
