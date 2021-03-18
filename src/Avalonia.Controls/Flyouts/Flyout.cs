using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;

#nullable enable

namespace Avalonia.Controls
{
    public class Flyout : FlyoutBase
    {
        public static readonly StyledProperty<object> ContentProperty =
            AvaloniaProperty.Register<Flyout, object>(nameof(Content));

        public Styles? FlyoutPresenterStyle
        {
            get
            {
                if (_styles == null)
                {
                    _styles = new Styles();
                    _styles.CollectionChanged += OnFlyoutPresenterStylesChanged;
                }

                return _styles;
            }
        }

        private Styles? _styles;
        private bool _stylesDirty;

        public object Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        protected override Control CreatePresenter()
        {
            return new FlyoutPresenter
            {
                [!ContentControl.ContentProperty] = this[!ContentProperty]
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

        private void OnFlyoutPresenterStylesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _stylesDirty = true;
        }
    }
}
