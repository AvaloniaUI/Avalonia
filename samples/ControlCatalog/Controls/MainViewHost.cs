using System;
using Avalonia;
using Avalonia.Controls;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Controls
{
    internal class MainViewHost : PageNavigationHost
    {
        private MainWindowViewModel _viewModel;

        protected override Type StyleKeyOverride => typeof(PageNavigationHost);

        public MainViewHost(MainWindowViewModel mainWindowViewModel)
        {
            _viewModel = mainWindowViewModel;
        }

        private void SettingsViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            ApplySettings();
        }

        private void ApplySettings()
        {
            if (TopLevel.GetTopLevel(this) is { } topLevel)
            {
                App.ApplyTopLevelTransparency(topLevel, _viewModel.SettingsViewModel.CurrentWindowTransparencyLevel);
                App.Current?.RequestedThemeVariant = _viewModel.SettingsViewModel.CurrentThemeVariant;
            }
        }

        private void MainViewHost_ThemeUpdated(object? sender, EventArgs e)
        {
            RecreatePage();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            (App.Current as App)?.ThemeUpdated += MainViewHost_ThemeUpdated;
            _viewModel.SettingsViewModel.PropertyChanged += SettingsViewModel_PropertyChanged;

            RecreatePage();
            ApplySettings();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            (App.Current as App)?.ThemeUpdated -= MainViewHost_ThemeUpdated;
            _viewModel.SettingsViewModel.PropertyChanged -= SettingsViewModel_PropertyChanged;
        }

        private void RecreatePage()
        {
            Page = new MainView()
            {
                DataContext = _viewModel
            };
        }
    }
}
