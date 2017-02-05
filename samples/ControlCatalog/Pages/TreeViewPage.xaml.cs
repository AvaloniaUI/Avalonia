using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public class TreeViewPage : UserControl
    {
        public TreeViewPage()
        {
            this.InitializeComponent();
            DataContext = new Node().Children;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public class Node
        {
            private IList<Node> _children;
            public string Header { get; private set; }
            public IList<Node> Children
            {
                get
                {
                    if (_children == null)
                    {
                        _children = Enumerable.Range(1, 10).Select(i => new Node() {Header = $"Item {i}"})
                            .ToArray();
                    }
                    return _children;
                }
            }
        }
    }
}
