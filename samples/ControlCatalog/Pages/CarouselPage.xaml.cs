using System;
using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;

namespace ControlCatalog.Pages
{
    public class CarouselItemViewModel : ReactiveObject
    {
        public CarouselItemViewModel(CarouselPageViewModel parent, string title)
        {
            Title = title;

            CloseCommand = ReactiveCommand.Create(() =>
            {
                parent.Items.Remove(this);
            });
        }
        
        public  ReactiveCommand<Unit, Unit> CloseCommand { get; }
        
        public string Title { get; set; }
    }
    
    public class CarouselPageViewModel : ReactiveObject
    {
        private CarouselItemViewModel _selectedItem;
        private ObservableCollection<CarouselItemViewModel> _items;
        private int i = 1;
        
        public CarouselPageViewModel()
        {
            _items = new ObservableCollection<CarouselItemViewModel>();

            AddItemCommand = ReactiveCommand.Create(() =>
            {
                Items.Add(new CarouselItemViewModel(this, $"{i++}"));
            });
        }
        
        public CarouselItemViewModel SelectedItem
        {
            get => _selectedItem;
            set => this.RaiseAndSetIfChanged(ref _selectedItem, value);
        }

        public ObservableCollection<CarouselItemViewModel> Items
        {
            get => _items;
            set => this.RaiseAndSetIfChanged(ref _items, value);
        }
        
        public ReactiveCommand<Unit, Unit> AddItemCommand { get; }
    }
    
    public class CarouselPage : UserControl
    {
        private Carousel _carousel;
        private Button _left;
        private Button _right;
        private ComboBox _transition;
        private ComboBox _orientation;

        public CarouselPage()
        {
            this.InitializeComponent();
           
            
            DataContext = new CarouselPageViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            //_carousel = this.FindControl<Carousel>("carousel");
            //_left = this.FindControl<Button>("left");
            //_right = this.FindControl<Button>("right");
            //_transition = this.FindControl<ComboBox>("transition");
            //_orientation = this.FindControl<ComboBox>("orientation");
        }

        private void TransitionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (_transition.SelectedIndex)
            {
                case 0:
                    _carousel.PageTransition = null;
                    break;
                case 1:
                    _carousel.PageTransition = new PageSlide(TimeSpan.FromSeconds(0.25), _orientation.SelectedIndex == 0 ? PageSlide.SlideAxis.Horizontal : PageSlide.SlideAxis.Vertical);
                    break;
                case 2:
                    _carousel.PageTransition = new CrossFade(TimeSpan.FromSeconds(0.25));
                    break;
            }
        }
    }
}
