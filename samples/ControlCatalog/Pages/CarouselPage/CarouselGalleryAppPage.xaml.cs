using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class CarouselGalleryAppPage : UserControl
    {
        private bool _syncing;
        private Point _dragStart;
        private bool _isDragging;
        private const double SwipeThreshold = 50;

        private ScrollViewer? _infoPanel;

        public CarouselGalleryAppPage()
        {
            InitializeComponent();
            _infoPanel = this.FindControl<ScrollViewer>("InfoPanel");
            HeroCarousel.SelectionChanged += OnHeroSelectionChanged;
            HeroPager.SelectedIndexChanged += OnPagerIndexChanged;
        }

        private void OnControlLoaded(object? sender, RoutedEventArgs e)
        {
            UpdateInfoPanelVisibility();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == BoundsProperty)
                UpdateInfoPanelVisibility();
        }

        private void UpdateInfoPanelVisibility()
        {
            if (_infoPanel != null)
                _infoPanel.IsVisible = Bounds.Width >= 640;
        }

        private void OnHamburgerClick(object? sender, RoutedEventArgs e)
        {
            RootDrawer.IsOpen = !RootDrawer.IsOpen;
        }

        private void OnHeroSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_syncing)
                return;
            _syncing = true;
            HeroPager.SelectedPageIndex = HeroCarousel.SelectedIndex;
            _syncing = false;
        }

        private void OnPagerIndexChanged(object? sender, PipsPagerSelectedIndexChangedEventArgs e)
        {
            if (_syncing)
                return;
            _syncing = true;
            HeroCarousel.SelectedIndex = e.NewIndex;
            _syncing = false;
        }

        private void OnDrawerMenuSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            RootDrawer.IsOpen = false;
            DrawerMenu.SelectedItem = null;
        }

        private void OnHeroPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
                return;
            _dragStart = e.GetPosition((Visual?)sender);
            _isDragging = true;
        }

        private void OnHeroPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (!_isDragging)
                return;
            _isDragging = false;
            var delta = e.GetPosition((Visual?)sender).X - _dragStart.X;
            if (Math.Abs(delta) < SwipeThreshold)
                return;
            if (delta < 0)
                HeroCarousel.Next();
            else
                HeroCarousel.Previous();
        }

        private void OnHeroPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            _isDragging = false;
        }
    }
}
