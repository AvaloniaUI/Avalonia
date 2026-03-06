using System;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class NavigationPageTransitionsPage : UserControl
    {
        private int _pageCount;

        public NavigationPageTransitionsPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            await DemoNav.PushAsync(NavigationDemoHelper.MakePage("Transitions", "Choose a transition type and push pages.", 0), null);
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

        private async void OnPush(object? sender, RoutedEventArgs e)
        {
            _pageCount++;
            await DemoNav.PushAsync(NavigationDemoHelper.MakePage($"Page {_pageCount}", $"Pushed with {GetTransitionName()}.", _pageCount));
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
    }
}
