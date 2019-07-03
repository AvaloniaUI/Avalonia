using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public class ItemsRepeaterPage : UserControl
    {
        private ItemsRepeater _repeater;
        private ScrollViewer _scroller;

        public ItemsRepeaterPage()
        {
            this.InitializeComponent();
            _repeater = this.FindControl<ItemsRepeater>("repeater");
            _scroller = this.FindControl<ScrollViewer>("scroller");
            DataContext = Enumerable.Range(1, 100000).Select(i => $"Item {i}" ).ToArray();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void LayoutChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_repeater == null)
            {
                return;
            }

            var comboBox = (ComboBox)sender;

            switch (comboBox.SelectedIndex)
            {
                case 0:
                    _scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                    _scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                    _repeater.Layout = new StackLayout { Orientation = Orientation.Vertical };
                    break;
                case 1:
                    _scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                    _scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                    _repeater.Layout = new StackLayout { Orientation = Orientation.Horizontal };
                    break;
                case 2:
                    _scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                    _scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    _repeater.Layout = new UniformGridLayout
                    {
                        Orientation = Orientation.Vertical,
                        MinItemWidth = 200,
                        MinItemHeight = 200,
                    };
                    break;
                case 3:
                    _scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    _scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                    _repeater.Layout = new UniformGridLayout
                    {
                        Orientation = Orientation.Horizontal,
                        MinItemWidth = 200,
                        MinItemHeight = 200,
                    };
                    break;
            }
        }
    }
}
