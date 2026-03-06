using System;
using Avalonia.Animation;
using Avalonia.Controls;

namespace ControlCatalog.Pages
{
    public partial class TabbedPageTransitionsPage : UserControl
    {
        public TabbedPageTransitionsPage()
        {
            InitializeComponent();
        }

        private void OnTransitionChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DemoTabs == null) return;

            DemoTabs.PageTransition = TransitionCombo?.SelectedIndex switch
            {
                1 => new CrossFade(TimeSpan.FromMilliseconds(250)),
                2 => new PageSlide(TimeSpan.FromMilliseconds(300), PageSlide.SlideAxis.Horizontal),
                3 => new PageSlide(TimeSpan.FromMilliseconds(300), PageSlide.SlideAxis.Vertical),
                4 => new CompositePageTransition
                {
                    PageTransitions =
                    {
                        new CrossFade(TimeSpan.FromMilliseconds(250)),
                        new PageSlide(TimeSpan.FromMilliseconds(300), PageSlide.SlideAxis.Horizontal)
                    }
                },
                _ => null
            };
        }

        private void OnPlacementChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DemoTabs == null) return;

            DemoTabs.TabPlacement = PlacementCombo?.SelectedIndex switch
            {
                1 => TabPlacement.Bottom,
                2 => TabPlacement.Left,
                3 => TabPlacement.Right,
                _ => TabPlacement.Top
            };
        }
    }
}
