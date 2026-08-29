using System;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class NavigationPageModalPage : UserControl
    {
        private bool _initialized;
        private int _modalCount;

        public NavigationPageModalPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (_initialized)
                return;

            _initialized = true;
            await DemoNav.PushAsync(NavigationDemoHelper.MakePage("Home", "Use Push Modal to show a modal on top.", 0), null);
        }

        private async void OnPushModal(object? sender, RoutedEventArgs e)
        {
            _modalCount++;
            var modal = NavigationDemoHelper.MakePage($"Modal {_modalCount}", "This page was presented modally.\nTap 'Pop Modal' to dismiss.", _modalCount);
            await DemoNav.PushModalAsync(modal);
            UpdateStatus();
        }

        private async void OnPopModal(object? sender, RoutedEventArgs e)
        {
            await DemoNav.PopModalAsync();
            UpdateStatus();
        }

        private async void OnPopAllModals(object? sender, RoutedEventArgs e)
        {
            await DemoNav.PopAllModalsAsync();
            _modalCount = 0;
            UpdateStatus();
        }

        private void OnTransitionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DemoNav == null)
                return;
            DemoNav.ModalTransition = TransitionCombo.SelectedIndex switch
            {
                1 => new CrossFade(TimeSpan.FromMilliseconds(250)),
                2 => null,
                _ => new PageSlide(TimeSpan.FromMilliseconds(300), PageSlide.SlideAxis.Vertical)
            };
        }

        private void UpdateStatus()
        {
            StatusText.Text = $"Modals: {DemoNav.ModalStack.Count}";
        }
    }
}
