using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;

namespace ControlCatalog.Pages
{
    public class TestItemViewModel
    {
        public string Name { get; set; }
    }

    public class TestItemViewModel2
    {
        public string Name { get; set; }
    }

    public class TestViewModel
    {
        public TestViewModel()
        {
            Items = new List<object>
            {
                new TestItemViewModel{ Name = "Item1"},
                new TestItemViewModel2{ Name="Items2"}
            };
        }

        public List<object> Items { get; set; }

        private TestItemViewModel _selectedItem;

        public TestItemViewModel SelectedItem
        {
            get { return _selectedItem; }
            set {  _selectedItem = value; }
        }
    }

    public class TreeViewPage : UserControl
    {
        public TreeViewPage()
        {
            this.InitializeComponent();
            DataContext = new TestViewModel(); //new Node().Children;
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
