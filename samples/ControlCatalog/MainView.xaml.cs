using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Styling;
using ControlCatalog.ViewModels;

namespace ControlCatalog
{
    public partial class MainView : DrawerPage
    {
        public MainView()
        {
            InitializeComponent();

            Loaded += MainView_Loaded;
            Unloaded += MainView_Unloaded;
        }

        private const double WideBreakpoint = 1008;
        private const double NarrowBreakpoint = 640;

        protected override Type StyleKeyOverride => typeof(MainView);

        private void MainView_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext == null)
                return;

            SizeChanged += OnDrawerSizeChanged;
            UpdateAdaptiveLayout();

            if (Application.Current is { } app)
            {
                app.RequestedThemeVariant = ThemeVariant.Default;
            }
        }

        private void MainView_Unloaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            SizeChanged -= OnDrawerSizeChanged;
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

            var width = Bounds.Width;
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

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (ViewModel != null)
            {
                ViewModel.Navigator = NavPage;

                ViewModel.NavigateToItem(ViewModel.HomeItem);
            }
        }

        internal MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (DataContext == null)
                return;

            UpdateAdaptiveLayout();

            var topLevel = TopLevel.GetTopLevel(this)!;

            var insets = topLevel.InsetsManager;
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
        }

        private async void AvaloniaIcon_OnTapped(object? sender, TappedEventArgs e)
        {
            await TopLevel.GetTopLevel(this)!.Launcher.LaunchUriAsync(new Uri("https://avaloniaui.net/"));
        }
    }
}
