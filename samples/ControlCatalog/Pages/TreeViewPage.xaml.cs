using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;

namespace ControlCatalog.Pages
{
    public class TreeViewPage : UserControl
    {
        public TreeViewPage()
        {
            InitializeComponent();
            DataContext = new PageViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private class PageViewModel : ReactiveObject
        {
            private SelectionMode _selectionMode;

            public PageViewModel()
            {
                Node root = new Node();
                Items = root.Children;
                Selection = new SelectionModel();

                AddItemCommand = ReactiveCommand.Create(() =>
                {
                    Node parentItem = Selection.SelectedItems.Count > 0 ?
                        (Node)Selection.SelectedItems[0] : root;
                    parentItem.AddNewItem();
                });

                RemoveItemCommand = ReactiveCommand.Create(() =>
                {
                    while (Selection.SelectedItems.Count > 0)
                    {
                        Node lastItem = (Node)Selection.SelectedItems[0];
                        RecursiveRemove(Items, lastItem);
                        Selection.DeselectAt(Selection.SelectedIndices[0]);
                    }

                    bool RecursiveRemove(ObservableCollection<Node> items, Node selectedItem)
                    {
                        if (items.Remove(selectedItem))
                        {
                            return true;
                        }

                        foreach (Node item in items)
                        {
                            if (item.AreChildrenInitialized && RecursiveRemove(item.Children, selectedItem))
                            {
                                return true;
                            }
                        }

                        return false;
                    }
                });
            }

            public ObservableCollection<Node> Items { get; }

            public SelectionModel Selection { get; }

            public ReactiveCommand<Unit, Unit> AddItemCommand { get; }

            public ReactiveCommand<Unit, Unit> RemoveItemCommand { get; }

            public SelectionMode SelectionMode
            {
                get => _selectionMode;
                set
                {
                    Selection.ClearSelection();
                    this.RaiseAndSetIfChanged(ref _selectionMode, value);
                }
            }
        }

        private class Node
        {
            private int _counter;
            private ObservableCollection<Node> _children;

            public string Header { get; private set; }

            public bool AreChildrenInitialized => _children != null;

            public ObservableCollection<Node> Children
            {
                get
                {
                    if (_children == null)
                    {
                        _children = new ObservableCollection<Node>(Enumerable.Range(1, 10).Select(i => CreateNewNode()));
                    }
                    return _children;
                }
            }

            public void AddNewItem() => Children.Add(CreateNewNode());

            public override string ToString() => Header;

            private Node CreateNewNode() => new Node { Header = $"Item {_counter++}" };
        }
    }
}
