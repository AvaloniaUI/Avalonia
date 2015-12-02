using System;
using System.Collections.ObjectModel;
using System.Linq;
using ReactiveUI;

namespace BindingTest.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        private string _booleanString = "True";
        private double _doubleValue = 5.0;
        private string _stringValue = "Simple Binding";

        public MainWindowViewModel()
        {
            Items = new ObservableCollection<TestItem>(
                Enumerable.Range(0, 20).Select(x => new TestItem
                {
                    StringValue = "Item " + x
                }));

            SelectedItems = new ObservableCollection<TestItem>();

            ShuffleItems = ReactiveCommand.Create();
            ShuffleItems.Subscribe(_ =>
            {
                var r = new Random();
                Items.Move(r.Next(Items.Count), 1);
            });
        }

        public ObservableCollection<TestItem> Items { get; }
        public ObservableCollection<TestItem> SelectedItems { get; }
        public ReactiveCommand<object> ShuffleItems { get; }
        public TestUserControlViewModel UserControl { get; } = new TestUserControlViewModel();

        public string BooleanString
        {
            get { return _booleanString; }
            set { this.RaiseAndSetIfChanged(ref _booleanString, value); }
        }

        public double DoubleValue
        {
            get { return _doubleValue; }
            set { this.RaiseAndSetIfChanged(ref _doubleValue, value); }
        }

        public string StringValue
        {
            get { return _stringValue; }
            set { this.RaiseAndSetIfChanged(ref _stringValue, value); }
        }
    }
}
