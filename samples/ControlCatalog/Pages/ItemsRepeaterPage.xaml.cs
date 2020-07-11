using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public class ItemsRepeaterPage : UserControl
    {
        private readonly ItemsRepeaterPageViewModel _viewModel;
        private ItemsRepeater _repeater;
        private ScrollViewer _scroller;
        private Button _scrollToLast;
        private Button _scrollToRandom;
        private Random _random = new Random(0);

        public ItemsRepeaterPage()
        {
            this.InitializeComponent();
            _repeater = this.FindControl<ItemsRepeater>("repeater");
            _scroller = this.FindControl<ScrollViewer>("scroller");
            _scrollToLast = this.FindControl<Button>("scrollToLast");
            _scrollToRandom = this.FindControl<Button>("scrollToRandom");
            _repeater.PointerPressed += RepeaterClick;
            _repeater.KeyDown += RepeaterOnKeyDown;
            _scrollToLast.Click += scrollToLast_Click;
            _scrollToRandom.Click += scrollToRandom_Click;
            DataContext = _viewModel = new ItemsRepeaterPageViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void OnSelectTemplateKey(object sender, SelectTemplateEventArgs e)
        {
            var item = (ItemsRepeaterPageViewModel.Item)e.DataContext;
            e.TemplateKey = (item.Index % 2 == 0) ? "even" : "odd";
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
                case 4:
                    _scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                    _scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    _repeater.Layout = new WrapLayout
                    {
                        Orientation = Orientation.Vertical,
                        HorizontalSpacing = 20,
                        VerticalSpacing = 20
                    };
                    break;
                case 5:
                    _scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    _scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                    _repeater.Layout = new WrapLayout
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalSpacing = 20,
                        VerticalSpacing = 20
                    };
                    break;
            }
        }

        private void ScrollTo(int index)
        {
            System.Diagnostics.Debug.WriteLine("Scroll to " + index);
            var layoutManager = ((Window)this.GetVisualRoot()).LayoutManager;
            var element = _repeater.GetOrCreateElement(index);
            layoutManager.ExecuteLayoutPass();
            element.BringIntoView();
        }

        private void RepeaterClick(object sender, PointerPressedEventArgs e)
        {
            var item = (e.Source as TextBlock)?.DataContext as ItemsRepeaterPageViewModel.Item;
            _viewModel.SelectedItem = item;
        }

        private void RepeaterOnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                _viewModel.ResetItems();
            }
        }

        private void scrollToLast_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ScrollTo(_viewModel.Items.Count - 1);
        }

        private void scrollToRandom_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ScrollTo(_random.Next(_viewModel.Items.Count - 1));
        }
    }
}
