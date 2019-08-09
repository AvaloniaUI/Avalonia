using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using ControlCatalog.ViewModels;

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
            _repeater.PointerPressed += RepeaterClick;
            DataContext = new ItemsRepeaterPageViewModel();
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

        private void RepeaterClick(object sender, PointerPressedEventArgs e)
        {
            var item = (e.Source as TextBlock)?.DataContext as string;
            ((ItemsRepeaterPageViewModel)DataContext).SelectedItem = item;
        }
    }
}
