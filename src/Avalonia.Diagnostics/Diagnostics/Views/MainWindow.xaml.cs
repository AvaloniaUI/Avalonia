using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Diagnostics;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
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
        private readonly IDisposable? _keySubscription;
        private readonly Dictionary<Popup, IDisposable> _frozenPopupStates;
        private AvaloniaObject? _root;

        public MainWindow()
        {
            InitializeComponent();

            _keySubscription = InputManager.Instance?.Process
                .OfType<RawKeyEventArgs>()
                .Where(x => x.Type == RawKeyEventType.KeyDown)
                .Subscribe(RawKeyDown);

            _frozenPopupStates = new Dictionary<Popup, IDisposable>();

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

        public AvaloniaObject? Root
        {
            get => _root;
            set
            {
                if (_root != value)
                {
                    if (_root is ICloseable oldClosable)
                    {
                        oldClosable.Closed -= RootClosed;
                    }

                    _root = value;

                    if (_root is  ICloseable newClosable)
                    {
                        newClosable.Closed += RootClosed;
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
            _keySubscription?.Dispose();

            foreach (var state in _frozenPopupStates)
            {
                state.Value.Dispose();
            }

            _frozenPopupStates.Clear();

            if (_root is ICloseable cloneable)
            {
                cloneable.Closed -= RootClosed;
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

        private static List<PopupRoot> GetPopupRoots(TopLevel root)
        {
            var popupRoots = new List<PopupRoot>();

            void ProcessProperty<T>(IControl control, AvaloniaProperty<T> property)
            {
                if (control.GetValue(property) is IPopupHostProvider popupProvider
                    && popupProvider.PopupHost is PopupRoot popupRoot)
                {
                    popupRoots.Add(popupRoot);
                }
            }

            foreach (var control in root.GetVisualDescendants().OfType<IControl>())
            {
                if (control is Popup p && p.Host is PopupRoot popupRoot)
                {
                    popupRoots.Add(popupRoot);
                }

                ProcessProperty(control, ContextFlyoutProperty);
                ProcessProperty(control, ContextMenuProperty);
                ProcessProperty(control, FlyoutBase.AttachedFlyoutProperty);
                ProcessProperty(control, ToolTipDiagnostics.ToolTipProperty);
                ProcessProperty(control, Button.FlyoutProperty);
            }

            return popupRoots;
        }

        private void RawKeyDown(RawKeyEventArgs e)
        {
            var vm = (MainViewModel?)DataContext;
            if (vm is null)
            {
                return;
            }

            var root = Root as TopLevel
                ?? vm.PointerOverRoot as TopLevel;
            if (root is null)
            {
                return;
            }

            switch (e.Modifiers)
            {
                case RawInputModifiers.Control when (e.Key == Key.LeftShift || e.Key == Key.RightShift):
                case RawInputModifiers.Shift when (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl):
                case RawInputModifiers.Shift | RawInputModifiers.Control:
                {
                    IControl? control = null;

                    foreach (var popupRoot in GetPopupRoots(root))
                    {
                        control = GetHoveredControl(popupRoot);

                        if (control != null)
                        {
                            break;
                        }
                    }

                    control ??= GetHoveredControl(root);

                    if (control != null)
                    {
                        vm.SelectControl(control);
                    }

                    break;
                }

                case RawInputModifiers.Control | RawInputModifiers.Alt when e.Key == Key.F:
                {
                    vm.FreezePopups = !vm.FreezePopups;

                    foreach (var popupRoot in GetPopupRoots(root))
                    {
                        if (popupRoot.Parent is Popup popup)
                        {
                            if (vm.FreezePopups)
                            {
                                var lightDismissEnabledState = popup.SetValue(
                                    Popup.IsLightDismissEnabledProperty,
                                    !vm.FreezePopups,
                                    BindingPriority.Animation);

                                if (lightDismissEnabledState != null)
                                {
                                    _frozenPopupStates[popup] = lightDismissEnabledState;
                                }
                            }
                            else
                            {
                                //TODO Use Dictionary.Remove(Key, out Value) in netstandard 2.1
                                if (_frozenPopupStates.ContainsKey(popup))
                                {
                                    _frozenPopupStates[popup].Dispose();
                                    _frozenPopupStates.Remove(popup);
                                }
                            }
                        }
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
