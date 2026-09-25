using System;
using Avalonia;
using Avalonia.Controls;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public partial class SettingsPage : ContentPage
    {
        private SettingsViewModel? _viewModel;

        public SettingsPage(SettingsViewModel settingsViewModel)
        {
            InitializeComponent();

            DataContext = settingsViewModel;
        }

        public SettingsPage()
        {
            InitializeComponent();
            DataContext = new SettingsViewModel();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (_viewModel is not null)
            {
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            _viewModel = null;

            if (DataContext is SettingsViewModel viewModel)
            {
                _viewModel = viewModel;
                _viewModel.CurrentCatalogTheme = App.CurrentTheme;

                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_viewModel is not { } viewModel)
                return;

            if (e.PropertyName == nameof(viewModel.CurrentCatalogTheme))
            {
                App.SetCatalogThemes(viewModel.CurrentCatalogTheme);
            }
            else if (TopLevel.GetTopLevel(this) is { } topLevel && e.PropertyName == nameof(viewModel.CurrentWindowTransparencyLevel))
            {
                App.ApplyTopLevelTransparency(topLevel, viewModel.CurrentWindowTransparencyLevel);
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (DataContext is SettingsViewModel viewModel)
            {
                var topLevel = TopLevel.GetTopLevel(this)!;
                if (topLevel is Window window)
                    viewModel.CurrentWindowDecorations = window.WindowDecorations;
            }
        }
    }
}
