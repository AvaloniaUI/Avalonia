﻿using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Diagnostics.Models;
using Avalonia.Input;
using Avalonia.Metadata;
using Avalonia.Threading;
using System.Linq;

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
        private bool _shouldVisualizeDirtyRects;
        private bool _showFpsOverlay;
        private bool _freezePopups;
        private string? _pointerOverElementName;
        private IInputRoot? _pointerOverRoot;
        private IScreenshotHandler? _screenshotHandler;
        private bool _showPropertyType;        
        private bool _showImplementedInterfaces;
        
        public MainViewModel(AvaloniaObject root)
        {
            _root = root;
            _logicalTree = new TreePageViewModel(this, LogicalTreeNode.Create(root));
            _visualTree = new TreePageViewModel(this, VisualTreeNode.Create(root));
            _events = new EventsPageViewModel(this);

            UpdateFocusedControl();

            if (KeyboardDevice.Instance is not null)
                KeyboardDevice.Instance.PropertyChanged += KeyboardPropertyChanged;
            SelectedTab = 0;
            if (root is TopLevel topLevel)
            {
                _pointerOverSubscription = topLevel.GetObservable(TopLevel.PointerOverElementProperty)
                    .Subscribe(x => PointerOverElement = x);

            }
            else
            {
#nullable disable
                _pointerOverSubscription = InputManager.Instance.PreProcess
                    .OfType<Input.Raw.RawPointerEventArgs>()
                    .Subscribe(e =>
                        {
                            PointerOverRoot = e.Root;
                            PointerOverElement = e.Root.InputHitTest(e.Position);
                        });
#nullable restore
            }
            Console = new ConsoleViewModel(UpdateConsoleContext);
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

        public bool ShouldVisualizeDirtyRects
        {
            get => _shouldVisualizeDirtyRects;
            set
            {
                var changed = true;
                if (_root is TopLevel topLevel && topLevel.Renderer is { })
                {
                    topLevel.Renderer.DrawDirtyRects = value;
                }
                else if (_root is Controls.Application app && app.RendererRoot is { })
                {
                    app.RendererRoot.DrawDirtyRects = value;
                }
                else
                {
                    changed = false;
                }
                if (changed)
                RaiseAndSetIfChanged(ref _shouldVisualizeDirtyRects, value);
            }
        }

        public void ToggleVisualizeDirtyRects()
        {
            ShouldVisualizeDirtyRects = !ShouldVisualizeDirtyRects;
        }

        public void ToggleVisualizeMarginPadding()
        {
            ShouldVisualizeMarginPadding = !ShouldVisualizeMarginPadding;
        }

        public bool ShowFpsOverlay
        {
            get => _showFpsOverlay;
            set
            {
                var changed = true;
                if (_root is TopLevel topLevel && topLevel.Renderer is { })
                {
                    topLevel.Renderer.DrawFps = value;
                }
                else if (_root is Controls.Application app && app.RendererRoot is { })
                {
                    app.RendererRoot.DrawFps = value;
                }
                else
                {
                    changed = false;
                }
                if(changed)
                    RaiseAndSetIfChanged(ref _showFpsOverlay, value);
            }
        }

        public void ToggleFpsOverlay()
        {
            ShowFpsOverlay = !ShowFpsOverlay;
        }

        public ConsoleViewModel Console { get; }

        public ViewModelBase? Content
        {
            get { return _content; }
            private set
            {
                if (_content is TreePageViewModel oldTree &&
                    value is TreePageViewModel newTree &&
                    oldTree?.SelectedNode?.Visual is IControl control)
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
            private  set => RaiseAndSetIfChanged( ref _pointerOverRoot , value); 
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

        private void UpdateConsoleContext(ConsoleContext context)
        {
            context.root = _root;

            if (Content is TreePageViewModel tree)
            {
                context.e = tree.SelectedNode?.Visual;
            }
        }

        public void SelectControl(IControl control)
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
            if (_root is TopLevel top)
            {
                top.Renderer.DrawDirtyRects = false;
                top.Renderer.DrawFps = false;
            }
        }

        private void UpdateFocusedControl()
        {
            FocusedControl = KeyboardDevice.Instance?.FocusedElement?.GetType().Name;
        }

        private void KeyboardPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(KeyboardDevice.Instance.FocusedElement))
            {
                UpdateFocusedControl();
            }
        }

        public void RequestTreeNavigateTo(IControl control, bool isVisualTree)
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
        bool CanShot(object? parameter)
        {
            return Content is TreePageViewModel tree
                && tree.SelectedNode != null
                && tree.SelectedNode.Visual is VisualTree.IVisual visual
                && visual.VisualRoot != null;
        }

        async void Shot(object? parameter)
        {
            if ((Content as TreePageViewModel)?.SelectedNode?.Visual is IControl control
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
        }

        public bool ShowImplementedInterfaces 
        { 
            get => _showImplementedInterfaces; 
            private set => RaiseAndSetIfChanged(ref _showImplementedInterfaces , value); 
        }

        public void ToggleShowImplementedInterfaces(object parametr)
        {
            ShowImplementedInterfaces = !ShowImplementedInterfaces;
            if (Content is TreePageViewModel viewModel)
            {
                viewModel.UpdatePropertiesView();
            }
        }

        public bool ShowDettailsPropertyType 
        { 
            get => _showPropertyType; 
            private set => RaiseAndSetIfChanged(ref  _showPropertyType , value); 
        }

        public void ToggleShowDettailsPropertyType(object paramter)
        {
            ShowDettailsPropertyType = !ShowDettailsPropertyType;
        }
    }
}
