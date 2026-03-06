using System;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class NavigationPageModalTransitionsPage : UserControl
    {
        private static readonly SolidColorBrush[] ModalColors =
        [
            new SolidColorBrush(Color.FromRgb(237, 231, 246)),
            new SolidColorBrush(Color.FromRgb(255, 243, 224)),
            new SolidColorBrush(Color.FromRgb(224, 247, 250)),
            new SolidColorBrush(Color.FromRgb(232, 245, 233)),
            new SolidColorBrush(Color.FromRgb(255, 235, 238)),
            new SolidColorBrush(Color.FromRgb(227, 242, 253)),
        ];

        private int _modalCount;

        public NavigationPageModalTransitionsPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            await DemoNav.PushAsync(new ContentPage
            {
                Header = "Modal Transitions",
                Content = new TextBlock
                {
                    Text = "Select a modal transition type and tap 'Open Modal'.",
                    FontSize = 13,
                    Opacity = 0.7,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    MaxWidth = 260,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                },
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            }, null);
            UpdateTransition();
        }

        private void OnTransitionChanged(object? sender, SelectionChangedEventArgs e) => UpdateTransition();

        private void OnDurationChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (DurationLabel == null)
                return;
            DurationLabel.Text = $"{(int)DurationSlider.Value} ms";
            UpdateTransition();
        }

        private async void OnOpenModal(object? sender, RoutedEventArgs e)
        {
            _modalCount++;
            var modal = new ContentPage
            {
                Header = $"Modal {_modalCount}",
                Background = ModalColors[_modalCount % ModalColors.Length],
                Content = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = $"Modal {_modalCount}",
                            FontSize = 18,
                            FontWeight = FontWeight.SemiBold,
                            HorizontalAlignment = HorizontalAlignment.Center
                        },
                        new TextBlock
                        {
                            Text = $"Presented with {GetTransitionName()}.",
                            FontSize = 13,
                            Opacity = 0.7,
                            TextWrapping = TextWrapping.Wrap,
                            TextAlignment = TextAlignment.Center,
                            MaxWidth = 240
                        }
                    }
                },
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };
            await DemoNav.PushModalAsync(modal);
            StatusText.Text = $"Modals: {DemoNav.ModalStack.Count}";
        }

        private async void OnPopModal(object? sender, RoutedEventArgs e)
        {
            await DemoNav.PopModalAsync();
            StatusText.Text = $"Modals: {DemoNav.ModalStack.Count}";
        }

        private void UpdateTransition()
        {
            if (DemoNav == null)
                return;
            var duration = TimeSpan.FromMilliseconds(DurationSlider.Value);
            DemoNav.ModalTransition = TransitionCombo.SelectedIndex switch
            {
                1 => new CrossFade(duration),
                2 => null,
                _ => new PageSlide(duration, PageSlide.SlideAxis.Vertical)
            };
        }

        private string GetTransitionName() => TransitionCombo.SelectedIndex switch
        {
            1 => "CrossFade",
            2 => "no transition",
            _ => "PageSlide (from Bottom)"
        };
    }
}
