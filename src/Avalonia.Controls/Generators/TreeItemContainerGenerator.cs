using System;
using System.Collections.Generic;

namespace Avalonia.Controls.Generators
{
    public class TreeItemContainerGenerator : ItemContainerGenerator
    {
        internal TreeItemContainerGenerator(TreeView owner)
            : base(owner)
        {
            Index = new TreeContainerIndex(owner);
        }

        public TreeContainerIndex Index { get; }
    }

    public class TreeContainerIndex
    {
        private readonly TreeView _owner;

        internal TreeContainerIndex(TreeView owner) => _owner = owner;

        [Obsolete("Use TreeView.GetRealizedTreeContainers")]
        public IEnumerable<Control> Containers => _owner.GetRealizedTreeContainers();

        [Obsolete("Use TreeView.TreeContainerFromItem")]
        public Control? ContainerFromItem(object item) => _owner.TreeContainerFromItem(item);

        [Obsolete("Use TreeView.TreeItemFromContainer")]
        public object? ItemFromContainer(Control container) => _owner.TreeItemFromContainer(container);
    }
}
