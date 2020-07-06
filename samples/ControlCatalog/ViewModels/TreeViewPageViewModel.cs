using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using Avalonia.Controls;
using ReactiveUI;

namespace ControlCatalog.ViewModels
{
    public class TreeViewPageViewModel : ReactiveObject
    {
        private readonly Node _root;
        private SelectionMode _selectionMode;

        public TreeViewPageViewModel()
        {
            _root = new Node();

            Items = _root.Children;
            Selection = new SelectionModel();
            Selection.SelectionChanged += SelectionChanged;

            AddItemCommand = ReactiveCommand.Create(AddItem);
            RemoveItemCommand = ReactiveCommand.Create(RemoveItem);
            SelectRandomItemCommand = ReactiveCommand.Create(SelectRandomItem);
        }

        public ObservableCollection<Node> Items { get; }
        public SelectionModel Selection { get; }
        public ReactiveCommand<Unit, Unit> AddItemCommand { get; }
        public ReactiveCommand<Unit, Unit> RemoveItemCommand { get; }
        public ReactiveCommand<Unit, Unit> SelectRandomItemCommand { get; }

        public SelectionMode SelectionMode
        {
            get => _selectionMode;
            set
            {
                Selection.ClearSelection();
                this.RaiseAndSetIfChanged(ref _selectionMode, value);
            }
        }

        private void AddItem()
        {
            var parentItem = Selection.SelectedItems.Count > 0 ? (Node)Selection.SelectedItems[0] : _root;
            parentItem.AddItem();
        }

        private void RemoveItem()
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
        }

        private void SelectRandomItem()
        {
            var random = new Random();
            var depth = random.Next(4);
            var indexes = Enumerable.Range(0, 4).Select(x => random.Next(10));
            var path = new IndexPath(indexes);
            Selection.SelectedIndex = path;
        }

        private void SelectionChanged(object sender, SelectionModelSelectionChangedEventArgs e)
        {
            var selected = string.Join(",", e.SelectedIndices);
            var deselected = string.Join(",", e.DeselectedIndices);
            System.Diagnostics.Debug.WriteLine($"Selected '{selected}', Deselected '{deselected}'");
        }

        public class Node
        {
            private ObservableCollection<Node> _children;
            private int _childIndex = 10;

            public Node()
            {
                Header = "Item";
            }

            public Node(Node parent, int index)
            {
                Parent = parent;
                Header = parent.Header + ' ' + index;
            }

            public Node Parent { get; }
            public string Header { get; }
            public bool AreChildrenInitialized => _children != null;
            public ObservableCollection<Node> Children => _children ??= CreateChildren();
            public void AddItem() => Children.Add(new Node(this, _childIndex++));
            public void RemoveItem(Node child) => Children.Remove(child);
            public override string ToString() => Header;

            private ObservableCollection<Node> CreateChildren()
            {
                return new ObservableCollection<Node>(
                    Enumerable.Range(0, 10).Select(i => new Node(this, i)));
            }
        }
    }
}
