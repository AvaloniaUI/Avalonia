using System;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class NavigationPageTransitionsPage : UserControl
    {
        private static readonly SolidColorBrush[] PageColors =
        [
            new SolidColorBrush(Color.FromRgb(232, 245, 233)),
            new SolidColorBrush(Color.FromRgb(227, 242, 253)),
            new SolidColorBrush(Color.FromRgb(255, 248, 225)),
            new SolidColorBrush(Color.FromRgb(243, 229, 245)),
            new SolidColorBrush(Color.FromRgb(224, 242, 241)),
            new SolidColorBrush(Color.FromRgb(255, 235, 238)),
        ];

        private int _pageCount;

        public NavigationPageTransitionsPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            await DemoNav.PushAsync(MakePage("Transitions", "Choose a transition type and push pages.", 0), null);
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

        private void OnPush(object? sender, RoutedEventArgs e)
        {
            _pageCount++;
            DemoNav.Push(MakePage($"Page {_pageCount}", $"Pushed with {GetTransitionName()}.", _pageCount));
        }

        private async void OnPop(object? sender, RoutedEventArgs e) => await DemoNav.PopAsync();

        private void UpdateTransition()
        {
            if (DemoNav == null)
                return;
            var duration = TimeSpan.FromMilliseconds(DurationSlider.Value);
            DemoNav.PageTransition = TransitionCombo.SelectedIndex switch
            {
                1 => new PageSlide(duration, PageSlide.SlideAxis.Horizontal),
                3 => new ParallaxSlideTransition(duration),
                4 => new CrossFade(duration),
                5 => new FadeThroughTransition(duration),
                6 => new PageSlideTransition(duration, PageSlideTransition.SlideAxis.Horizontal),
                7 => new PageSlideTransition(duration, PageSlideTransition.SlideAxis.Vertical),
                8 => new CompositeTransition(duration),
                9 => null,
                _ => new PageSlide(duration, PageSlide.SlideAxis.Horizontal)
            };
        }

        private string GetTransitionName() => TransitionCombo.SelectedIndex switch
        {
            1 => "Page Slide",
            3 => "Parallax Slide",
            4 => "Cross Fade",
            5 => "Fade Through",
            6 => "Page Slide (Horizontal)",
            7 => "Page Slide (Vertical)",
            8 => "Composite (Slide + Fade)",
            9 => "no transition",
            _ => "Page Slide"
        };

        private static ContentPage MakePage(string header, string body, int index) =>
            new ContentPage
            {
                Header = header,
                Background = PageColors[index % PageColors.Length],
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
                            FontSize = 18,
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
                            MaxWidth = 240
                        }
                    }
                },
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };
    }
}
