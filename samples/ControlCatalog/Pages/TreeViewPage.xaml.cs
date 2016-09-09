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
            DataContext = CreateNodes(0);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private IList<Node> CreateNodes(int level)
        {
            return Enumerable.Range(0, 10).Select(x => new Node
            {
                Header = $"Item {x}",
                Children = level < 5 ? CreateNodes(level + 1) : null,
            }).ToList();
        }

        private class Node
        {
            public string Header { get; set; }
            public IList<Node> Children { get; set; }
        }
    }
}
