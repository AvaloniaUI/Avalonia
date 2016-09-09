using System;
using System.Collections.ObjectModel;
using System.Linq;
using ReactiveUI;
using System.Reactive.Linq;

namespace BindingTest.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        private string _booleanString = "True";
        private double _doubleValue = 5.0;
        private string _stringValue = "Simple Binding";
        private bool _booleanFlag = false;

        public MainWindowViewModel()
        {
            Items = new ObservableCollection<TestItem>(
                Enumerable.Range(0, 20).Select(x => new TestItem
                {
                    StringValue = "Item " + x,
                    Detail = "Item " + x + " details",
                }));

            SelectedItems = new ObservableCollection<TestItem>();

            ShuffleItems = ReactiveCommand.Create();
            ShuffleItems.Subscribe(_ =>
            {
                var r = new Random();
                Items.Move(r.Next(Items.Count), 1);
            });

            StringValueCommand = ReactiveCommand.Create();
            StringValueCommand.Subscribe(param =>
            {
                BooleanFlag = !BooleanFlag;
                StringValue = param.ToString();
            });
        }

        public ObservableCollection<TestItem> Items { get; }
        public ObservableCollection<TestItem> SelectedItems { get; }
        public ReactiveCommand<object> ShuffleItems { get; }

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

        public bool BooleanFlag
        {
            get { return _booleanFlag; }
            set { this.RaiseAndSetIfChanged(ref _booleanFlag, value); }
        }

        public ReactiveCommand<object> StringValueCommand { get; }

        public DataAnnotationsErrorViewModel DataAnnotationsValidation { get; } = new DataAnnotationsErrorViewModel();
        public ExceptionErrorViewModel ExceptionDataValidation { get; } = new ExceptionErrorViewModel();
        public IndeiErrorViewModel IndeiDataValidation { get; } = new IndeiErrorViewModel();
    }
}
