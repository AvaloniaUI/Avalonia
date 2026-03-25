using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace ControlCatalog.Pages
{
    public partial class NavigationPageScrollAwarePage : UserControl
    {
        // Minimal IObserver wrapper: avoids a System.Reactive dependency.
        private sealed class ActionObserver<T>(Action<T> onNext) : IObserver<T>
        {
            public void OnNext(T value) => onNext(value);
            public void OnError(Exception error) { }
            public void OnCompleted() { }
        }

        private IDisposable? _scrollSubscription;
        private ScrollViewer? _scrollViewer;
        private double _lastScrollY;
        private double _currentTranslateY;
        private bool _initialized;

        public NavigationPageScrollAwarePage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (_initialized)
            {
                if (_scrollViewer != null)
                    Dispatcher.UIThread.Post(() => AttachScrollWatcher(_scrollViewer), DispatcherPriority.Loaded);
                return;
            }

            _initialized = true;

            _scrollViewer = new ScrollViewer { Content = BuildLongContent() };

            var rootPage = new ContentPage
            {
                Header = "Scroll to Hide Bar",
                Content = _scrollViewer,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
            };

            NavigationPage.SetBarLayoutBehavior(rootPage, BarLayoutBehavior.Overlay);
            await DemoNav.PushAsync(rootPage, null);

            Dispatcher.UIThread.Post(() => AttachScrollWatcher(_scrollViewer), DispatcherPriority.Loaded);
        }

        private void OnUnloaded(object? sender, RoutedEventArgs e) => DetachScrollWatcher();

        private void AttachScrollWatcher(ScrollViewer? sv)
        {
            if (sv == null)
                return;

            DetachScrollWatcher();

            var navBar = DemoNav.GetVisualDescendants().OfType<Border>()
                .FirstOrDefault(b => b.Name == "PART_NavigationBar");
            if (navBar == null)
                return;
            var transform = new TranslateTransform();
            navBar.RenderTransform = transform;

            _lastScrollY = 0;
            _currentTranslateY = 0;

            _scrollSubscription = sv.GetObservable(ScrollViewer.OffsetProperty)
                .Subscribe(new ActionObserver<Vector>(offset =>
                {
                    double y = offset.Y;
                    double delta = y - _lastScrollY;
                    _lastScrollY = y;

                    double barHeight = DemoNav.BarHeight;

                    if (y <= barHeight)
                        _currentTranslateY = Math.Clamp(-y, -barHeight, 0);
                    else if (delta > 0)
                        _currentTranslateY = Math.Clamp(_currentTranslateY - delta, -barHeight, 0);
                    else if (delta < 0)
                        _currentTranslateY = Math.Clamp(_currentTranslateY - delta, -barHeight, 0);

                    transform.Y = _currentTranslateY;
                }));
        }

        private void DetachScrollWatcher()
        {
            _scrollSubscription?.Dispose();
            _scrollSubscription = null;

            var navBar = DemoNav.GetVisualDescendants().OfType<Border>()
                .FirstOrDefault(b => b.Name == "PART_NavigationBar");
            if (navBar?.RenderTransform is TranslateTransform t)
                t.Y = 0;
        }

        private static Control BuildLongContent()
        {
            string[] items =
            [
                "Scroll down to hide the navigation bar.",
                "Scroll back up to reveal it again.",
                "The bar tracks the scroll position for a smooth effect.",
                "BarLayoutBehavior.Overlay lets content extend behind the bar.",
                "GetObservable(ScrollViewer.OffsetProperty) drives the scroll detection.",
                "TranslateTransform.Y is clamped between -BarHeight and 0.",
                "Bar always reveals fully when scrolled back to the top.",
                "Cleanup disposes the observable subscription on Unload.",
                "Keep scrolling…",
                "Item 10", "Item 11", "Item 12", "Item 13", "Item 14", "Item 15",
                "Item 16", "Item 17", "Item 18", "Item 19",
                "Item 20, now try scrolling back up!",
            ];

            var stack = new StackPanel { Spacing = 1 };
            for (int i = 0; i < items.Length; i++)
            {
                stack.Children.Add(new Border
                {
                    Background = i % 2 == 0
                        ? new SolidColorBrush(Color.FromArgb(12, 128, 128, 128))
                        : Brushes.Transparent,
                    Padding = new Avalonia.Thickness(16, 20),
                    Margin = i == 0 ? new Avalonia.Thickness(0, 52, 0, 0) : new Avalonia.Thickness(0),
                    Child = new StackPanel
                    {
                        Spacing = 4,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = $"Item {i + 1}",
                                FontSize = 12,
                                FontWeight = FontWeight.SemiBold,
                                Opacity = 0.4,
                            },
                            new TextBlock
                            {
                                Text = items[i],
                                FontSize = 14,
                                TextWrapping = TextWrapping.Wrap,
                            },
                        }
                    }
                });
            }

            return stack;
        }
    }
}
