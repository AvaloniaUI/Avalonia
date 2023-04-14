using System.ComponentModel;
using Avalonia.Controls.Primitives;
using Avalonia.Metadata;
using Avalonia.Styling;

namespace Avalonia.Controls
{
    public class Flyout : PopupFlyoutBase
    {
        /// <summary>
        /// Defines the <see cref="Content"/> property
        /// </summary>
        public static readonly StyledProperty<object> ContentProperty =
            AvaloniaProperty.Register<Flyout, object>(nameof(Content));

        private Classes? _classes;

        /// <summary>
        /// Gets the Classes collection to apply to the FlyoutPresenter this Flyout is hosting
        /// </summary>
        public Classes FlyoutPresenterClasses => _classes ??= new Classes();

        /// <summary>
        /// Defines the <see cref="FlyoutPresenterTheme"/> property.
        /// </summary>
        public static readonly StyledProperty<ControlTheme?> FlyoutPresenterThemeProperty =
            AvaloniaProperty.Register<Flyout, ControlTheme?>(nameof(FlyoutPresenterTheme));
        
        /// <summary>
        /// Gets or sets the <see cref="ControlTheme"/> that is applied to the container element generated for the flyout presenter.
        /// </summary>
        public ControlTheme? FlyoutPresenterTheme
        {
            get => GetValue(FlyoutPresenterThemeProperty); 
            set => SetValue(FlyoutPresenterThemeProperty, value);
        }
        
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
