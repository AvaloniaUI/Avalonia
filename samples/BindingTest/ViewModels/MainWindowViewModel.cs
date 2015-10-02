using System;
using System.Collections.ObjectModel;
using ReactiveUI;

namespace BindingTest.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        private string _booleanString = "True";
        private string _stringValue = "Simple Binding";

        public MainWindowViewModel()
        {
            Items = new ObservableCollection<TestItem>
            {
                new TestItem { StringValue = "Foo" },
                new TestItem { StringValue = "Bar" },
                new TestItem { StringValue = "Baz" },
            };

            ShuffleItems = ReactiveCommand.Create();
            ShuffleItems.Subscribe(_ =>
            {
                var r = new Random();
                Items[1] = Items[r.Next(Items.Count)];
            });
        }

        public ObservableCollection<TestItem> Items { get; }
        public ReactiveCommand<object> ShuffleItems { get; }

        public string BooleanString
        {
            get { return _booleanString; }
            set { this.RaiseAndSetIfChanged(ref _booleanString, value); }
        }

        public string StringValue
        {
            get { return _stringValue; }
            set { this.RaiseAndSetIfChanged(ref _stringValue, value); }
        }
    }
}
