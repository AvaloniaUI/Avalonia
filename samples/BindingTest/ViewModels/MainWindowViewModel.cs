using System;
using System.Collections.ObjectModel;
using ReactiveUI;

namespace BindingTest.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        private string _simpleBinding = "Simple Binding";

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

        public string SimpleBinding
        {
            get { return _simpleBinding; }
            set { this.RaiseAndSetIfChanged(ref _simpleBinding, value); }
        }
    }
}
