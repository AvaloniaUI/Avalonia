using System;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Avalonia.Diagnostics.Views
{
    internal class MainWindow : Window, IStyleHost
    {
        private readonly IDisposable _keySubscription;
        private TopLevel? _root;

        public MainWindow()
        {
            InitializeComponent();

            _keySubscription = InputManager.Instance.Process
                .OfType<RawKeyEventArgs>()
                .Subscribe(RawKeyDown);

            EventHandler? lh = default;
            lh = (s, e) =>
            {
                this.Opened -= lh;
                if ((DataContext as MainViewModel)?.StartupScreenIndex is { } index)
                {
                    var screens = this.Screens;
                    if (index > -1 && index < screens.ScreenCount)
                    {
                        var screen = screens.All[index];
                        this.Position = screen.Bounds.TopLeft;
                        this.WindowState = WindowState.Maximized;
                    }
                }
            };
            this.Opened += lh;
        }

        public TopLevel? Root
        {
            get => _root;
            set
            {
                if (_root != value)
                {
                    if (_root != null)
                    {
                        _root.Closed -= RootClosed;
                    }

                    _root = value;

                    if (_root != null)
                    {
                        _root.Closed += RootClosed;
                        DataContext = new MainViewModel(_root);
                    }
                    else
                    {
                        DataContext = null;
                    }
                }
            }
        }

        IStyleHost? IStyleHost.StylingParent => null;

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _keySubscription.Dispose();

            if (_root != null)
            {
                _root.Closed -= RootClosed;
                _root = null;
            }

            ((MainViewModel?)DataContext)?.Dispose();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private IControl? GetHoveredControl(TopLevel topLevel)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var point = (topLevel as IInputRoot)?.MouseDevice?.GetPosition(topLevel) ?? default;
#pragma warning restore CS0618 // Type or member is obsolete                

            return (IControl?)topLevel.GetVisualsAt(point, x =>
                {
                    if (x is AdornerLayer || !x.IsVisible)
                    {
                        return false;
                    }

                    return !(x is IInputElement ie) || ie.IsHitTestVisible;
                })
                .FirstOrDefault();
        }

        private void RawKeyDown(RawKeyEventArgs e)
        {
            var vm = (MainViewModel?)DataContext;
            if (vm is null)
            {
                return;
            }

            switch (e.Modifiers)
            {
                case RawInputModifiers.Control | RawInputModifiers.Shift:
                {
                    IControl? control = null;

                    foreach (var popup in Root.GetVisualDescendants().OfType<Popup>())
                    {
                        if (popup.Host?.HostedVisualTreeRoot is PopupRoot popupRoot)
                        {
                            control = GetHoveredControl(popupRoot);

                            if (control != null)
                            {
                                break;
                            }
                        }
                    }

                    control ??= GetHoveredControl(Root);

                    if (control != null)
                    {
                        vm.SelectControl(control);
                    }

                    break;
                }
                case RawInputModifiers.Alt when e.Key == Key.S || e.Key == Key.D:
                {
                    vm.EnableSnapshotStyles(e.Key == Key.S);

                    break;
                }
            }
        }

        private void RootClosed(object? sender, EventArgs e) => Close();

        public void SetOptions(DevToolsOptions options) =>
            (DataContext as MainViewModel)?.SetOptions(options);
    }
}
