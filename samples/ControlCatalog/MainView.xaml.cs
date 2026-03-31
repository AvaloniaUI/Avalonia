using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using ControlCatalog.Models;
using ControlCatalog.ViewModels;

namespace ControlCatalog
{
    public partial class MainView : UserControl
    {
        private Action? _disposeTransparencySetters;

        public MainView()
        {
            InitializeComponent();

            Loaded += MainView_Loaded;
            Unloaded += MainView_Unloaded;
        }

        private const double WideBreakpoint = 1008;
        private const double NarrowBreakpoint = 640;

        private void MainView_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext == null)
                return;

            MainDrawer.SizeChanged += OnDrawerSizeChanged;
            UpdateAdaptiveLayout();
        }

        private void MainView_Unloaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            MainDrawer.SizeChanged -= OnDrawerSizeChanged;
            _lastAppliedMode = null;
        }

        private SplitViewDisplayMode? _lastAppliedMode;
        private bool _updatingLayout;

        private void OnDrawerSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
                UpdateAdaptiveLayout();
        }

        private void UpdateAdaptiveLayout()
        {
            if (_updatingLayout || DataContext == null)
                return;

            var width = MainDrawer.Bounds.Width;
            if (width <= 0)
                return;

            SplitViewDisplayMode targetMode;
            if (width >= WideBreakpoint)
                targetMode = SplitViewDisplayMode.Inline;
            else if (width >= NarrowBreakpoint)
                targetMode = SplitViewDisplayMode.CompactInline;
            else
                targetMode = SplitViewDisplayMode.Overlay;

            if (_lastAppliedMode == targetMode)
                return;

            _updatingLayout = true;
            try
            {
                _lastAppliedMode = targetMode;
                ViewModel.DisplayMode = targetMode;

                if (targetMode == SplitViewDisplayMode.Inline)
                    ViewModel.IsDrawerOpened = true;
                else if (targetMode == SplitViewDisplayMode.Overlay)
                    ViewModel.IsDrawerOpened = false;
            }
            finally
            {
                _updatingLayout = false;
            }
        }

        private void Themes_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is CatalogTheme theme)
            {
                App.SetCatalogThemes(theme);
            }
        }

        private void ThemeVariants_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (Application.Current is { } app && e.AddedItems.Count > 0 && e.AddedItems[0] is ThemeVariant themeVariant)
            {
                app.RequestedThemeVariant = themeVariant;
            }
        }

        private void FlowDirection_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (TopLevel.GetTopLevel(this) is { } topLevel && e.AddedItems.Count > 0 && e.AddedItems[0] is FlowDirection flowDirection)
            {
                topLevel.FlowDirection = flowDirection;
            }
        }

        private void Decorations_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (TopLevel.GetTopLevel(this) is Window window && e.AddedItems.Count > 0 && e.AddedItems[0] is WindowDecorations systemDecorations)
            {
                window.WindowDecorations = systemDecorations;
            }
        }

        private void TransparencyLevels_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            _disposeTransparencySetters?.Invoke();

            if (TopLevel.GetTopLevel(this) is { } topLevel && e.AddedItems.Count > 0 && e.AddedItems[0] is WindowTransparencyLevel transparencyLevel)
            {
                topLevel.TransparencyLevelHint = [transparencyLevel];

                if (topLevel.ActualTransparencyLevel != WindowTransparencyLevel.None &&
                    topLevel.ActualTransparencyLevel == transparencyLevel)
                {
                    var transparentBrush = new ImmutableSolidColorBrush(Colors.White, 0);
                    var semiTransparentBrush = new ImmutableSolidColorBrush(Colors.Gray, 0.2);
                    _disposeTransparencySetters =
                        (Action)topLevel.SetValue(BackgroundProperty, transparentBrush, Avalonia.Data.BindingPriority.Style)!.Dispose +
                        MainDrawer.SetValue(BackgroundProperty, semiTransparentBrush, Avalonia.Data.BindingPriority.Style)!.Dispose +
                        MainDrawer.SetValue(DrawerPage.DrawerBackgroundProperty, semiTransparentBrush, Avalonia.Data.BindingPriority.Style)!.Dispose;
                }
            }
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (ViewModel != null)
            {
                ViewModel.Navigator = NavPage;
            }
        }

        internal MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (DataContext == null)
                return;

            UpdateAdaptiveLayout();

            if (TopLevel.GetTopLevel(this) is Window window)
                ViewModel.SelectedDecorationIndex = (int)window.WindowDecorations;

            var insets = TopLevel.GetTopLevel(this)!.InsetsManager;
            if (insets != null)
            {
                // In real life application these events should be unsubscribed to avoid memory leaks.
                ViewModel.SafeAreaPadding = insets.SafeAreaPadding;
                insets.SafeAreaChanged += (sender, args) =>
                {
                    ViewModel.SafeAreaPadding = insets.SafeAreaPadding;
                };

                ViewModel.DisplayEdgeToEdge = insets.DisplayEdgeToEdgePreference;
                ViewModel.IsSystemBarVisible = insets.IsSystemBarVisible ?? true;

                ViewModel.PropertyChanged += async (sender, args) =>
                {
                    if (args.PropertyName == nameof(ViewModel.DisplayEdgeToEdge))
                    {
                        insets.DisplayEdgeToEdgePreference = ViewModel.DisplayEdgeToEdge;
                    }
                    else if (args.PropertyName == nameof(ViewModel.IsSystemBarVisible))
                    {
                        insets.IsSystemBarVisible = ViewModel.IsSystemBarVisible;
                    }

                    // Give the OS some time to apply new values and refresh the view model.
                    await Task.Delay(100);
                    ViewModel.DisplayEdgeToEdge = insets.DisplayEdgeToEdgePreference;
                    ViewModel.IsSystemBarVisible = insets.IsSystemBarVisible ?? true;
                };
            }

            ViewModel.SelectedPageIndex = 0;
        }
    }
}
