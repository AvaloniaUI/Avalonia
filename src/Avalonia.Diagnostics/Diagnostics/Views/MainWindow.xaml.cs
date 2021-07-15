using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Diagnostics;
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

        private static IEnumerable<PopupRoot> GetPopupRoots(IVisual root)
        {
            foreach (var control in root.GetVisualDescendants().OfType<IControl>())
            {
                if (control is Popup { Host: PopupRoot r0 })
                {
                    yield return r0;
                }

                if (control.GetValue(ContextFlyoutProperty) is IPopupHostProvider { PopupHost: PopupRoot r1 })
                {
                    yield return r1;
                }

                if (control.GetValue(FlyoutBase.AttachedFlyoutProperty) is IPopupHostProvider { PopupHost: PopupRoot r2 })
                {
                    yield return r2;
                }

                if (control.GetValue(ToolTipDiagnostics.ToolTipProperty) is IPopupHostProvider { PopupHost: PopupRoot r3 })
                {
                    yield return r3;
                }

                if (control.GetValue(ContextMenuProperty) is IPopupHostProvider { PopupHost: PopupRoot r4 })
                {
                    yield return r4;
                }
            }
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

                    foreach (var popupRoot in GetPopupRoots(Root))
                    {
                        control = GetHoveredControl(popupRoot);

                        if (control != null)
                        {
                            break;
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
