using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages.Composition
{
    public partial class CompositionHomePage : ContentPage
    {
        private static readonly (string Group, string Title, string Description, Func<UserControl> Factory)[] Demos =
        {
            ("Explore", "Implicit Animations",
                "Attach implicit animations to visual elements.",
                () => new ImplicitAnimationsPage()),

            ("Explore", "Explicit Animations",
                "Attach explicit animations to visual elements.",
                () => new ExplicitAnimationsPage()),

            ("Explore", "Custom Animations",
                "Attach custom animations to visual elements.",
                () => new CustomAnimationPage()),

            ("Explore", "Brush Animations",
                "Animate various brushes on the composition thread",
                () => new BrushAnimationsPage()),
        };

        public CompositionHomePage()
        {
            InitializeComponent();

            DataContext = new CompositionAnimationViewModel();

            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            await SampleNav.PushAsync(NavigationDemoHelper.CreateGalleryHomePage(SampleNav, Demos), null);
        }
    }
}
