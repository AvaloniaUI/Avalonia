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

        public Styles? FlyoutPresenterStyle
        {
            get
            {
                if (_styles == null)
                {
                    _styles = new Styles();
                    _styles.CollectionChanged += OnMenuFlyoutPresenterStyleChanged;
                }

                return _styles;
            }
        }

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

        private Styles? _styles;
        private bool _stylesDirty = true;
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
            if (_styles != null && _stylesDirty)
            {
                // Presenter for flyout generally shouldn't be public, so
                // we should be ok to just reset the styles
                _popup.Child.Styles.Clear();
                _popup.Child.Styles.Add(_styles);
            }
            base.OnOpened();
        }

        private void OnMenuFlyoutPresenterStyleChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _stylesDirty = true;
        }
    }
}
