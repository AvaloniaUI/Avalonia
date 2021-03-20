using Avalonia.Controls.Primitives;
using Avalonia.Metadata;

#nullable enable

namespace Avalonia.Controls
{
    public class Flyout : FlyoutBase
    {
        /// <summary>
        /// Defines the <see cref="Content"/> property
        /// </summary>
        public static readonly StyledProperty<object> ContentProperty =
            AvaloniaProperty.Register<Flyout, object>(nameof(Content));

        /// <summary>
        /// Gets the Classes collection to applied to the FlyoutPresenter this Flyout is hosting
        /// </summary>
        public Classes? FlyoutPresenterClasses => _classes ??= new Classes();

        private Classes? _classes;

        /// <summary>
        /// Gets or sets the content to display in this flyout
        /// </summary>
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
                //Remove any classes no longer in use, ignoring any pseudoclasses
                for (int i = _popup.Child.Classes.Count - 1; i >= 0; i--)
                {
                    if (!FlyoutPresenterClasses.Contains(_popup.Child.Classes[i]) && 
                        !_popup.Child.Classes[i].Contains(":"))
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
