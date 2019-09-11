using System;
using System.Collections.ObjectModel;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public class CarouselPage : UserControl
    {
        private Carousel _carousel;
        private Button _left;
        private Button _right;
        private Button _add;

        public CarouselPage()
        {
            this.InitializeComponent();

            var vm = new ViewModel();
            DataContext = vm;
            _left.Click += (s, e) => _carousel.Previous();
            _right.Click += (s, e) => _carousel.Next();
            _add.Click += (s, e) => vm.Items.Add("boo");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            _carousel = this.FindControl<Carousel>("carousel");
            _left = this.FindControl<Button>("left");
            _right = this.FindControl<Button>("right");
            _add = this.FindControl<Button>("add");
        }

        private class ViewModel
        {
            public ViewModel()
            {
                Items = new ObservableCollection<string> { "foo", "bar", "baz" };
            }

            public ObservableCollection<string> Items { get; }

            public object SelectedItem { get; set; } = "foo";
        }
    }
}
