using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using Avalonia.Controls;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    public class TreeViewPageViewModel : ViewModelBase
    {
        private readonly Node _root;
        private SelectionMode _selectionMode;

        public TreeViewPageViewModel()
        {
            _root = new Node();

            Items = _root.Children;
            SelectedItems = new ObservableCollection<Node>();

            AddItemCommand = MiniCommand.Create(AddItem);
            RemoveItemCommand = MiniCommand.Create(RemoveItem);
            SelectRandomItemCommand = MiniCommand.Create(SelectRandomItem);
        }

        public ObservableCollection<Node> Items { get; }
        public ObservableCollection<Node> SelectedItems { get; }
        public MiniCommand AddItemCommand { get; }
        public MiniCommand RemoveItemCommand { get; }
        public MiniCommand SelectRandomItemCommand { get; }

        public SelectionMode SelectionMode
        {
            get => _selectionMode;
            set
            {
                SelectedItems.Clear();
                this.RaiseAndSetIfChanged(ref _selectionMode, value);
            }
        }

        private void AddItem()
        {
            var parentItem = SelectedItems.Count > 0 ? (Node)SelectedItems[0] : _root;
            parentItem.AddItem();
        }

        private void RemoveItem()
        {
            while (SelectedItems.Count > 0)
            {
                Node lastItem = (Node)SelectedItems[0];
                RecursiveRemove(Items, lastItem);
                SelectedItems.RemoveAt(0);
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
            var indexes = Enumerable.Range(0, depth).Select(x => random.Next(10));
            var node = _root;

            foreach (var i in indexes)
            {
                node = node.Children[i];
            }

            SelectedItems.Clear();
            SelectedItems.Add(node);
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
