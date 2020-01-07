using System.Collections.ObjectModel;
using System.Linq;

namespace ControlCatalog.ViewModels
{
    public class TreeNode
    {
        private int _counter;
        private ObservableCollection<TreeNode> _children;

        public string Header { get; private set; }

        public bool AreChildrenInitialized => _children != null;

        public ObservableCollection<TreeNode> Children
        {
            get
            {
                if (_children == null)
                {
                    _children = new ObservableCollection<TreeNode>(Enumerable.Range(1, 10).Select(i => CreateNewNode()));
                }
                return _children;
            }
        }

        public void AddNewItem() => Children.Add(CreateNewNode());

        public override string ToString() => Header;

        private TreeNode CreateNewNode() => new TreeNode { Header = $"Item {_counter++}" };
    }
}
