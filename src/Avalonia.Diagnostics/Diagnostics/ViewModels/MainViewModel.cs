using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Diagnostics.Models;
using Avalonia.Input;
using Avalonia.Metadata;
using Avalonia.Threading;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class MainViewModel : ViewModelBase, IDisposable
    {
        private readonly TopLevel _root;
        private readonly TreePageViewModel _logicalTree;
        private readonly TreePageViewModel _visualTree;
        private readonly EventsPageViewModel _events;
        private readonly IDisposable _pointerOverSubscription;
        private ViewModelBase _content;
        private int _selectedTab;
        private string _focusedControl;
        private string _pointerOverElement;
        private bool _shouldVisualizeMarginPadding = true;
        private bool _shouldVisualizeDirtyRects;
        private bool _showFpsOverlay;
        private IDisposable _selectedNodeChanged;

        public MainViewModel(TopLevel root)
        {
            _root = root;
            _logicalTree = new TreePageViewModel(this, LogicalTreeNode.Create(root));
            _visualTree = new TreePageViewModel(this, VisualTreeNode.Create(root));
            _events = new EventsPageViewModel(root);

            UpdateFocusedControl();
            KeyboardDevice.Instance.PropertyChanged += KeyboardPropertyChanged;
            SelectedTab = 0;
            _pointerOverSubscription = root.GetObservable(TopLevel.PointerOverElementProperty)
                .Subscribe(x => PointerOverElement = x?.GetType().Name);
            Console = new ConsoleViewModel(UpdateConsoleContext);
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
                _root.Renderer.DrawDirtyRects = value;
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
                _root.Renderer.DrawFps = value;
                RaiseAndSetIfChanged(ref _showFpsOverlay, value);
            }
        }

        public void ToggleFpsOverlay()
        {
            ShowFpsOverlay = !ShowFpsOverlay;
        }

        public ConsoleViewModel Console { get; }

        public ViewModelBase Content
        {
            get { return _content; }
            private set
            {
                TreePageViewModel oldTree = _content as TreePageViewModel;
                TreePageViewModel newTree = value as TreePageViewModel;
                if (oldTree != null)
                {
                    _selectedNodeChanged?.Dispose();
                    _selectedNodeChanged = null;
                }

                if (newTree != null)
                {
                    if (oldTree != null &&
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
                            TimeSpan.FromMilliseconds(0),
                            DispatcherPriority.ApplicationIdle);
                    }
                    _selectedNodeChanged = Observable
                        .FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                            x => newTree.PropertyChanged += x,
                            x => newTree.PropertyChanged -= x
                        ).Subscribe(arg => RaisePropertyChanged(nameof(TreePageViewModel.SelectedNode)));
                }


                RaiseAndSetIfChanged(ref _content, value);
            }
        }

        public int SelectedTab
        {
            get { return _selectedTab; }
            set
            {
                _selectedTab = value;

                switch (value)
                {
                    case 0:
                        Content = _logicalTree;
                        break;
                    case 1:
                        Content = _visualTree;
                        break;
                    case 2:
                        Content = _events;
                        break;
                }

                RaisePropertyChanged();
            }
        }

        public string FocusedControl
        {
            get { return _focusedControl; }
            private set { RaiseAndSetIfChanged(ref _focusedControl, value); }
        }

        public string PointerOverElement
        {
            get { return _pointerOverElement; }
            private set { RaiseAndSetIfChanged(ref _pointerOverElement, value); }
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

        public void Dispose()
        {
            KeyboardDevice.Instance.PropertyChanged -= KeyboardPropertyChanged;
            _pointerOverSubscription.Dispose();
            _selectedNodeChanged?.Dispose();
            _logicalTree.Dispose();
            _visualTree.Dispose();
            _root.Renderer.DrawDirtyRects = false;
            _root.Renderer.DrawFps = false;
        }

        private void UpdateFocusedControl()
        {
            FocusedControl = KeyboardDevice.Instance.FocusedElement?.GetType().Name;
        }

        private void KeyboardPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(KeyboardDevice.Instance.FocusedElement))
            {
                UpdateFocusedControl();
            }
        }

        [DependsOn(nameof(TreePageViewModel.SelectedNode))]
        [DependsOn(nameof(Content))]
        bool CanShot(object paramter)
        {
            return Content is TreePageViewModel tree
                && tree.SelectedNode != null
                && tree.SelectedNode.Visual != null
                && tree.SelectedNode.Visual.VisualRoot != null;
        }

        void Shot(object parameter)
        {
            // This is a workaround because MethodToCommand does not support the asynchronous method.
            Task.Factory.StartNew(arg =>
                {
                    if (arg is IControl control)
                    {
                        try
                        {
                            var folder = GetScreenShotDirectory(_root);
                            if (System.IO.Directory.Exists(folder) == false)
                            {
                                System.IO.Directory.CreateDirectory(folder);
                            }
                            var filePath = System.IO.Path.Combine(folder
                                , $"{DateTime.Now:yyyyMMddhhmmssfff}.png");

                            var output = new System.IO.FileStream(filePath, System.IO.FileMode.Create);
                            Dispatcher.UIThread.Post(() =>
                                {
                                    control.RenderTo(output);
                                    output.Dispose();
                                }
                            );

                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(ex.Message);
                            //TODO: Notify error
                        }

                    }


                }, (Content as TreePageViewModel)?.SelectedNode?.Visual);
        }

        /// <summary>
        /// Return the path of the screenshot folder according to the rules indicated in issue <see href="https://github.com/AvaloniaUI/Avalonia/issues/4743">GH-4743</see>
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        string GetScreenShotDirectory(TopLevel root)
        {
            var rootType = root.GetType();
            var windowName = rootType.Name;
            var assembly = Assembly.GetExecutingAssembly();
            var appName = Application.Current?.Name
                ?? assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product
                ?? assembly.GetName().Name;
            var appVerions = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion
                ?? assembly.GetCustomAttribute<AssemblyVersionAttribute>().Version;
            return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures, Environment.SpecialFolderOption.Create)
                , "ScreenShots"
                , appName
                , appVerions
                , windowName);
        }
    }
}
