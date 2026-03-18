using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class CarouselGesturesPage : UserControl
    {
        private bool _keyboardEnabled = true;

        public CarouselGesturesPage()
        {
            InitializeComponent();
            DemoCarousel.KeyDown += OnKeyDown;
            DemoCarousel.SelectionChanged += OnSelectionChanged;
            DemoCarousel.Loaded += (_, _) => DemoCarousel.Focus();
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (!_keyboardEnabled)
                return;

            switch (e.Key)
            {
                case Key.Left:
                case Key.Up:
                    DemoCarousel.Previous();
                    LastActionText.Text = $"Action: Key {e.Key} (Previous)";
                    e.Handled = true;
                    break;
                case Key.Right:
                case Key.Down:
                    DemoCarousel.Next();
                    LastActionText.Text = $"Action: Key {e.Key} (Next)";
                    e.Handled = true;
                    break;
            }
        }

        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            StatusText.Text = $"Item: {DemoCarousel.SelectedIndex + 1} / {DemoCarousel.ItemCount}";
        }

        private void OnSwipeEnabledChanged(object? sender, RoutedEventArgs e)
        {
            DemoCarousel.IsSwipeEnabled = SwipeCheck.IsChecked == true;
        }

        private void OnWrapSelectionChanged(object? sender, RoutedEventArgs e)
        {
            DemoCarousel.WrapSelection = WrapCheck.IsChecked == true;
        }

        private void OnKeyboardEnabledChanged(object? sender, RoutedEventArgs e)
        {
            _keyboardEnabled = KeyboardCheck.IsChecked == true;
        }
    }
}
