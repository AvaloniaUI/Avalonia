using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;

namespace ControlCatalog.Pages
{
    public partial class WideGamutPage : ContentPage
    {
        private IColorManagedPresentation? _presentation;

        public WideGamutPage()
        {
            InitializeComponent();

            RequestedText.Text = (AvaloniaLocator.Current.GetService<PresentationOptions>()?.PreferredColorSpace
                ?? PresentationColorSpace.Unspecified).ToString();

            // Only a desktop lifetime can open a second window to compare against.
            OpenPanel.IsVisible =
                Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime;
        }

        /// <summary>
        /// The color space is read once, when a window is created, so an open window can not change
        /// it. Rebinding the options and opening a new window gives that window the new color space
        /// while the windows already on screen keep theirs, which is what makes them comparable.
        /// </summary>
        private void OpenInColorSpace(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: string name }
                || !Enum.TryParse<PresentationColorSpace>(name, out var colorSpace))
                return;

            AvaloniaLocator.CurrentMutable.Bind<PresentationOptions>().ToConstant(
                new PresentationOptions { PreferredColorSpace = colorSpace });

            new Window
            {
                Title = $"Wide Gamut - {name}",
                Width = 760,
                Height = 560,
                Content = new WideGamutPage()
            }.Show();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            _presentation = TopLevel.GetTopLevel(this)?.PlatformImpl?.TryGetFeature<IColorManagedPresentation>();

            if (_presentation is not null)
                _presentation.CurrentColorSpaceChanged += OnCurrentColorSpaceChanged;

            UpdateCurrentColorSpace();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            if (_presentation is not null)
            {
                _presentation.CurrentColorSpaceChanged -= OnCurrentColorSpaceChanged;
                _presentation = null;
            }

            base.OnDetachedFromVisualTree(e);
        }

        private void OnCurrentColorSpaceChanged(object? sender, EventArgs e) => UpdateCurrentColorSpace();

        private void UpdateCurrentColorSpace()
        {
            CurrentText.Text = _presentation is null
                ? "not available, this backend has no IColorManagedPresentation"
                : _presentation.CurrentColorSpace.ToString();
        }
    }
}
