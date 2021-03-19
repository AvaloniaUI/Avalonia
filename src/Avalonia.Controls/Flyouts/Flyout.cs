using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Avalonia.Controls.Primitives;
using Avalonia.Metadata;
using Avalonia.Styling;

#nullable enable

namespace Avalonia.Controls
{
    public class Flyout : FlyoutBase
    {
        public static readonly StyledProperty<object> ContentProperty =
            AvaloniaProperty.Register<Flyout, object>(nameof(Content));

        public Classes? FlyoutPresenterClasses => _classes ??= new Classes();

        private Classes? _classes;

        [Content]
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
