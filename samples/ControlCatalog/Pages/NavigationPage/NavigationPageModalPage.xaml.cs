using System;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class NavigationPageModalPage : UserControl
    {
        private static readonly Color[] PageColors =
        {
            Color.Parse("#BBDEFB"), Color.Parse("#C8E6C9"), Color.Parse("#FFE0B2"),
            Color.Parse("#E1BEE7"), Color.Parse("#FFCDD2"), Color.Parse("#B2EBF2"),
        };

        private int _modalCount;

        public NavigationPageModalPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            await DemoNav.PushAsync(MakePage("Home", "Tap 'Push Modal' to present a modal page.", 0), null);
        }

        private async void OnPushModal(object? sender, RoutedEventArgs e)
        {
            _modalCount++;
            var modal = MakePage($"Modal {_modalCount}", "This page was presented modally.\nTap 'Pop Modal' to dismiss.", _modalCount);
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
                1 => new CrossFade(TimeSpan.FromMilliseconds(300)),
                2 => null,
                _ => new PageSlide(TimeSpan.FromMilliseconds(300), PageSlide.SlideAxis.Vertical)
            };
        }

        private void UpdateStatus()
        {
            StatusText.Text = $"Modals: {DemoNav.ModalStack.Count}";
        }

        private static ContentPage MakePage(string header, string body, int index) =>
            new ContentPage
            {
                Header = header,
                Background = new SolidColorBrush(PageColors[index % PageColors.Length]),
                Content = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = header,
                            FontSize = 20,
                            FontWeight = FontWeight.SemiBold,
                            HorizontalAlignment = HorizontalAlignment.Center
                        },
                        new TextBlock
                        {
                            Text = body,
                            FontSize = 13,
                            Opacity = 0.7,
                            TextWrapping = TextWrapping.Wrap,
                            TextAlignment = TextAlignment.Center,
                            MaxWidth = 260
                        }
                    }
                },
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };
    }
}
