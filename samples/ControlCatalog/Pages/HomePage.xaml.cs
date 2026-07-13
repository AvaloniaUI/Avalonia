using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public partial class HomePage : ContentPage
    {
        public HomePage()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            // Pages hosted in a NavigationPage inherit the navigator as DataContext;
            // the card grid binds to MainWindowViewModel, so resolve it from MainView.
            if (DataContext is not MainWindowViewModel)
                DataContext = this.FindAncestorOfType<MainView>()?.DataContext;

            StartFloatingAnimation(FloatIcon1, 12, 8, TimeSpan.FromSeconds(12));
            StartFloatingAnimation(FloatIcon2, -14, 8, TimeSpan.FromSeconds(10));
            StartFloatingAnimation(FloatIcon3, -12, -12, TimeSpan.FromSeconds(14));
            StartFloatingAnimation(FloatIcon4, 12, -8, TimeSpan.FromSeconds(11));
        }

        private static void StartFloatingAnimation(Control target, double dx, double dy, TimeSpan duration)
        {
            var transform = new TranslateTransform();
            target.RenderTransform = transform;

            var animation = new Animation
            {
                Duration = duration,
                IterationCount = IterationCount.Infinite,
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0d),
                        Setters =
                        {
                            new Setter(TranslateTransform.XProperty, 0d),
                            new Setter(TranslateTransform.YProperty, 0d),
                        }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(0.5d),
                        Setters =
                        {
                            new Setter(TranslateTransform.XProperty, dx),
                            new Setter(TranslateTransform.YProperty, dy),
                        }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1d),
                        Setters =
                        {
                            new Setter(TranslateTransform.XProperty, 0d),
                            new Setter(TranslateTransform.YProperty, 0d),
                        }
                    },
                }
            };

            _ = animation.RunAsync(target);
        }
    }
}
