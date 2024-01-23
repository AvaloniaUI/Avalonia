using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Diagnostics.Models;
using Avalonia.Input;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.Reactive;
using Avalonia.Rendering;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Controls.Primitives;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class MainViewModel : ViewModelBase, IDisposable
    {
        private readonly AvaloniaObject _root;
        private readonly TreePageViewModel _logicalTree;
        private readonly TreePageViewModel _visualTree;
        private readonly EventsPageViewModel _events;
        private readonly IDisposable _pointerOverSubscription;
        private ViewModelBase? _content;
        private int _selectedTab;
        private string? _focusedControl;
        private IInputElement? _pointerOverElement;
        private bool _shouldVisualizeMarginPadding = true;
        private bool _freezePopups;
        private string? _pointerOverElementName;
        private IInputRoot? _pointerOverRoot;
        private IScreenshotHandler? _screenshotHandler;
        private bool _showPropertyType;
        private bool _showImplementedInterfaces;
        private readonly HashSet<string> _pinnedProperties = new();
        private IBrush? _FocusHighlighter;
        private IDisposable? _currentFocusHighlightAdorner = default;

        public MainViewModel(AvaloniaObject root)
        {
            _root = root;
            _logicalTree = new TreePageViewModel(this, LogicalTreeNode.Create(root), _pinnedProperties);
            _visualTree = new TreePageViewModel(this, VisualTreeNode.Create(root), _pinnedProperties);
            _events = new EventsPageViewModel(this);

            UpdateFocusedControl();

            if (KeyboardDevice.Instance is not null)
                KeyboardDevice.Instance.PropertyChanged += KeyboardPropertyChanged;
            SelectedTab = 0;
            if (root is TopLevel topLevel)
            {
                _pointerOverRoot = topLevel;
                _pointerOverSubscription = topLevel.GetObservable(TopLevel.PointerOverElementProperty)
                    .Subscribe(x => PointerOverElement = x);

            }
            else
            {
                _pointerOverSubscription = InputManager.Instance!.PreProcess
                    .Subscribe(e =>
                        {
                            if (e is Input.Raw.RawPointerEventArgs pointerEventArgs)
                            {
                                PointerOverRoot = pointerEventArgs.Root;
                                PointerOverElement = pointerEventArgs.Root.InputHitTest(pointerEventArgs.Position);
                            }
                        });
            }
        }

        public bool FreezePopups
        {
            get => _freezePopups;
            set => RaiseAndSetIfChanged(ref _freezePopups, value);
        }

        public bool ShouldVisualizeMarginPadding
        {
            get => _shouldVisualizeMarginPadding;
            set => RaiseAndSetIfChanged(ref _shouldVisualizeMarginPadding, value);
        }

        public void ToggleVisualizeMarginPadding()
            => ShouldVisualizeMarginPadding = !ShouldVisualizeMarginPadding;

        private IRenderer? TryGetRenderer()
            => _root switch
            {
                TopLevel topLevel => topLevel.Renderer,
                Controls.Application app => app.RendererRoot,
                _ => null
            };

        private bool GetDebugOverlay(RendererDebugOverlays overlay)
            => ((TryGetRenderer()?.Diagnostics.DebugOverlays ?? RendererDebugOverlays.None) & overlay) != 0;

        private void SetDebugOverlay(RendererDebugOverlays overlay, bool enable,
            [CallerMemberName] string? propertyName = null)
        {
            if (TryGetRenderer() is not { } renderer)
            {
                return;
            }

            var oldValue = renderer.Diagnostics.DebugOverlays;
            var newValue = enable ? oldValue | overlay : oldValue & ~overlay;

            if (oldValue == newValue)
            {
                return;
            }

            renderer.Diagnostics.DebugOverlays = newValue;
            RaisePropertyChanged(propertyName);
        }

        public bool ShowDirtyRectsOverlay
        {
            get => GetDebugOverlay(RendererDebugOverlays.DirtyRects);
            set => SetDebugOverlay(RendererDebugOverlays.DirtyRects, value);
        }

        public void ToggleDirtyRectsOverlay()
            => ShowDirtyRectsOverlay = !ShowDirtyRectsOverlay;

        public bool ShowFpsOverlay
        {
            get => GetDebugOverlay(RendererDebugOverlays.Fps);
            set => SetDebugOverlay(RendererDebugOverlays.Fps, value);
        }

        public void ToggleFpsOverlay()
            => ShowFpsOverlay = !ShowFpsOverlay;

        public bool ShowLayoutTimeGraphOverlay
        {
            get => GetDebugOverlay(RendererDebugOverlays.LayoutTimeGraph);
            set => SetDebugOverlay(RendererDebugOverlays.LayoutTimeGraph, value);
        }

        public void ToggleLayoutTimeGraphOverlay()
            => ShowLayoutTimeGraphOverlay = !ShowLayoutTimeGraphOverlay;

        public bool ShowRenderTimeGraphOverlay
        {
            get => GetDebugOverlay(RendererDebugOverlays.RenderTimeGraph);
            set => SetDebugOverlay(RendererDebugOverlays.RenderTimeGraph, value);
        }

        public void ToggleRenderTimeGraphOverlay()
            => ShowRenderTimeGraphOverlay = !ShowRenderTimeGraphOverlay;

        public ViewModelBase? Content
        {
            get { return _content; }
            private set
            {
                if (_content is TreePageViewModel oldTree &&
                    value is TreePageViewModel newTree &&
                    oldTree?.SelectedNode?.Visual is Control control)
                {
                    // HACK: We want to select the currently selected control in the new tree, but
                    // to select nested nodes in TreeView, currently the TreeView has to be able to
                    // expand the parent nodes. Because at this point the TreeView isn't visible,
                    // this will fail unless we schedule the selection to run after layout.
                    DispatcherTimer.RunOnce(
                        () =>
                        {
                            try
                            {
                                newTree.SelectControl(control);
                            }
                            catch { }
                        },
                        TimeSpan.FromMilliseconds(0));
                }

                RaiseAndSetIfChanged(ref _content, value);
            }
        }

        public int SelectedTab
        {
            get { return _selectedTab; }
            // [MemberNotNull(nameof(_content))]
            set
            {
                _selectedTab = value;

                switch (value)
                {
                    case 1:
                        Content = _visualTree;
                        break;
                    case 2:
                        Content = _events;
                        break;
                    default:
                        Content = _logicalTree;
                        break;
                }

                RaisePropertyChanged();
            }
        }

        public string? FocusedControl
        {
            get { return _focusedControl; }
            private set { RaiseAndSetIfChanged(ref _focusedControl, value); }
        }

        public IInputRoot? PointerOverRoot
        {
            get => _pointerOverRoot;
            private set => RaiseAndSetIfChanged(ref _pointerOverRoot, value);
        }

        public IInputElement? PointerOverElement
        {
            get { return _pointerOverElement; }
            private set
            {
                RaiseAndSetIfChanged(ref _pointerOverElement, value);
                PointerOverElementName = value?.GetType()?.Name;
            }
        }

        public string? PointerOverElementName
        {
            get => _pointerOverElementName;
            private set => RaiseAndSetIfChanged(ref _pointerOverElementName, value);
        }

        public void SelectControl(Control control)
        {
            var tree = Content as TreePageViewModel;

            tree?.SelectControl(control);
        }

        public void EnableSnapshotStyles(bool enable)
        {
            if (Content is TreePageViewModel treeVm && treeVm.Details != null)
            {
                treeVm.Details.SnapshotStyles = enable;
            }
        }

        public void Dispose()
        {
            if (KeyboardDevice.Instance is not null)
                KeyboardDevice.Instance.PropertyChanged -= KeyboardPropertyChanged;
            _pointerOverSubscription.Dispose();
            _logicalTree.Dispose();
            _visualTree.Dispose();
            _currentFocusHighlightAdorner?.Dispose();
            if (TryGetRenderer() is { } renderer)
            {
                renderer.Diagnostics.DebugOverlays = RendererDebugOverlays.None;
            }
        }

        private void UpdateFocusedControl()
        {
            var element = KeyboardDevice.Instance?.FocusedElement;
            FocusedControl = element?.GetType().Name;
            _currentFocusHighlightAdorner?.Dispose();
            if (FocusHighlighter is IBrush brush
                && element is InputElement input
                && TopLevel.GetTopLevel(input) is { } topLevel
                && (topLevel is not Views.MainWindow))
            {
                if (topLevel is PopupRoot pr && pr.ParentTopLevel is Views.MainWindow)
                {
                    return;
                }
                _currentFocusHighlightAdorner = Controls.ControlHighlightAdorner.Add(input, brush);
            }
        }

        private void KeyboardPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(KeyboardDevice.Instance.FocusedElement))
            {
                UpdateFocusedControl();
            }
        }

        public void RequestTreeNavigateTo(Control control, bool isVisualTree)
        {
            var tree = isVisualTree ? _visualTree : _logicalTree;

            var node = tree.FindNode(control);

            if (node != null)
            {
                SelectedTab = isVisualTree ? 1 : 0;

                tree.SelectControl(control);
            }
        }

        public int? StartupScreenIndex { get; private set; } = default;

        [DependsOn(nameof(TreePageViewModel.SelectedNode))]
        [DependsOn(nameof(Content))]
        public bool CanShot(object? parameter)
        {
            return Content is TreePageViewModel tree
                && tree.SelectedNode != null
                && tree.SelectedNode.Visual is Visual visual
                && visual.VisualRoot != null;
        }

        public async void Shot(object? parameter)
        {
            if ((Content as TreePageViewModel)?.SelectedNode?.Visual is Control control
                && _screenshotHandler is { }
                )
            {
                try
                {
                    await _screenshotHandler.Take(control);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    //TODO: Notify error
                }
            }
        }

        public void SetOptions(DevToolsOptions options)
        {
            _screenshotHandler = options.ScreenshotHandler;
            StartupScreenIndex = options.StartupScreenIndex;
            ShowImplementedInterfaces = options.ShowImplementedInterfaces;
            FocusHighlighter = options.FocusHighlighterBrush;
            SelectedTab = (int)options.LaunchView;
        }

        public bool ShowImplementedInterfaces
        {
            get => _showImplementedInterfaces;
            private set => RaiseAndSetIfChanged(ref _showImplementedInterfaces, value);
        }

        public void ToggleShowImplementedInterfaces(object parameter)
        {
            ShowImplementedInterfaces = !ShowImplementedInterfaces;
            if (Content is TreePageViewModel viewModel)
            {
                viewModel.UpdatePropertiesView();
            }
        }

        public bool ShowDetailsPropertyType
        {
            get => _showPropertyType;
            private set => RaiseAndSetIfChanged(ref _showPropertyType, value);
        }

        public void ToggleShowDetailsPropertyType(object parameter)
        {
            ShowDetailsPropertyType = !ShowDetailsPropertyType;
        }

        public IBrush? FocusHighlighter
        {
            get => _FocusHighlighter;
            private set => RaiseAndSetIfChanged(ref _FocusHighlighter, value);
        }

        public void SelectFocusHighlighter(object parameter)
        {
            FocusHighlighter = parameter as IBrush;
        }
    }
}
