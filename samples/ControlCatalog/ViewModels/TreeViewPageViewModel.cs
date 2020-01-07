using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia.Controls;
using ReactiveUI;

namespace ControlCatalog.ViewModels
{
    public class TreeViewPageViewModel : ReactiveObject
    {
        private SelectionMode _selectionMode;
        private int _containerCount;

        public TreeViewPageViewModel()
        {
            TreeNode root = new TreeNode();
            Items = root.Children;
            SelectedItems = new ObservableCollection<TreeNode>();

            AddItemCommand = ReactiveCommand.Create(() =>
            {
                TreeNode parentItem = SelectedItems.Count > 0 ? SelectedItems[0] : root;
                parentItem.AddNewItem();
            });

            RemoveItemCommand = ReactiveCommand.Create(() =>
            {
                while (SelectedItems.Count > 0)
                {
                    TreeNode lastItem = SelectedItems[0];
                    RecursiveRemove(Items, lastItem);
                    SelectedItems.Remove(lastItem);
                }

                bool RecursiveRemove(ObservableCollection<TreeNode> items, TreeNode selectedItem)
                {
                    if (items.Remove(selectedItem))
                    {
                        return true;
                    }

                    foreach (TreeNode item in items)
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

        public ObservableCollection<TreeNode> Items { get; }

        public ObservableCollection<TreeNode> SelectedItems { get; }

        public ReactiveCommand<Unit, Unit> AddItemCommand { get; }

        public ReactiveCommand<Unit, Unit> RemoveItemCommand { get; }

        public SelectionMode SelectionMode
        {
            get => _selectionMode;
            set
            {
                SelectedItems.Clear();
                this.RaiseAndSetIfChanged(ref _selectionMode, value);
            }
        }

        public int ContainerCount
        {
            get => _containerCount;
            set => this.RaiseAndSetIfChanged(ref _containerCount, value);
        }
    }
}
