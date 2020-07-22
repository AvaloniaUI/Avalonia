using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Diagnostics.Models;
using Avalonia.Input;
using Avalonia.Threading;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class MainViewModel : ViewModelBase, IDisposable
    {
        private readonly IControl _root;
        private readonly TreePageViewModel _logicalTree;
        private readonly TreePageViewModel _visualTree;
        private readonly EventsPageViewModel _events;
        private readonly IDisposable _pointerOverSubscription;
        private ViewModelBase _content;
        private int _selectedTab;
        private string _focusedControl;
        private string _pointerOverElement;

        public MainViewModel(IControl root)
        {
            _root = root;
            _logicalTree = new TreePageViewModel(LogicalTreeNode.Create(root));
            _visualTree = new TreePageViewModel(VisualTreeNode.Create(root));
            _events = new EventsPageViewModel(root);

            UpdateFocusedControl();
            KeyboardDevice.Instance.PropertyChanged += KeyboardPropertyChanged;
            SelectedTab = 0;
            _pointerOverSubscription = root.GetObservable(TopLevel.PointerOverElementProperty)
                .Subscribe(x => PointerOverElement = x?.GetType().Name);
            Console = new ConsoleViewModel(UpdateConsoleContext);
        }

        public ConsoleViewModel Console { get; }

        public ViewModelBase Content
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
                        TimeSpan.FromMilliseconds(0),
                        DispatcherPriority.ApplicationIdle);
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

            if (tree != null)
            {
                tree.SelectControl(control);
            }
        }

        public void Dispose()
        {
            KeyboardDevice.Instance.PropertyChanged -= KeyboardPropertyChanged;
            _pointerOverSubscription.Dispose();
            _logicalTree.Dispose();
            _visualTree.Dispose();
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
    }
}
