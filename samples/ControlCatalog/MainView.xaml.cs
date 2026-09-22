using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Styling;
using ControlCatalog.ViewModels;

namespace ControlCatalog
{
    public partial class MainView : DrawerPage
    {
        private const double WideBreakpoint = 1008;
        private const double NarrowBreakpoint = 640;

        private SplitViewDisplayMode? _lastAppliedMode;
        private bool _updatingLayout;
        private IInsetsManager? _insets;

        public MainView()
        {
            InitializeComponent();

            Loaded += MainView_Loaded;
            Unloaded += MainView_Unloaded;
        }

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
                _insets = insets;
                ViewModel.SafeAreaPadding = insets.SafeAreaPadding;
                insets.SafeAreaChanged += OnSafeAreaChanged;

                ViewModel.DisplayEdgeToEdge = insets.DisplayEdgeToEdgePreference;
                ViewModel.IsSystemBarVisible = insets.IsSystemBarVisible ?? true;

                ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            if (_insets != null)
            {
                _insets.SafeAreaChanged -= OnSafeAreaChanged;
                _insets = null;
            }

            if (DataContext != null)
            {
                ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }
        }

        private void OnSafeAreaChanged(object? sender, SafeAreaChangedArgs e)
        {
            if (_insets != null)
            {
                ViewModel.SafeAreaPadding = _insets.SafeAreaPadding;
            }
        }

        private async void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            if (_insets is not { } insets)
            {
                return;
            }

            if (args.PropertyName == nameof(ViewModel.DisplayEdgeToEdge))
            {
                insets.DisplayEdgeToEdgePreference = ViewModel.DisplayEdgeToEdge;
            }
            else if (args.PropertyName == nameof(ViewModel.IsSystemBarVisible))
            {
                insets.IsSystemBarVisible = ViewModel.IsSystemBarVisible;
            }
            else
            {
                return;
            }

            // Give the OS some time to apply new values and refresh the view model.
            await Task.Delay(100);
            ViewModel.DisplayEdgeToEdge = insets.DisplayEdgeToEdgePreference;
            ViewModel.IsSystemBarVisible = insets.IsSystemBarVisible ?? true;
        }

        private async void AvaloniaIcon_OnTapped(object? sender, TappedEventArgs e)
        {
            await TopLevel.GetTopLevel(this)!.Launcher.LaunchUriAsync(new Uri("https://avaloniaui.net/"));
        }
    }
}
