using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Threading;
using ReactiveUI;

namespace BindingDemo.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        private string _booleanString = "True";
        private double _doubleValue = 5.0;
        private string _stringValue = "Simple Binding";
        private bool _booleanFlag = false;
        private string _currentTime;
        private NestedCommandViewModel _nested;

        public MainWindowViewModel()
        {
            Items = new ObservableCollection<TestItem>(
                Enumerable.Range(0, 20).Select(x => new TestItem
                {
                    StringValue = "Item " + x,
                    Detail = "Item " + x + " details",
                }));

            SelectedItems = new ObservableCollection<TestItem>();

            ShuffleItems = ReactiveCommand.Create(() =>
            {
                var r = new Random();
                Items.Move(r.Next(Items.Count), 1);
            });

            StringValueCommand = ReactiveCommand.Create<object>(param =>
            {
                BooleanFlag = !BooleanFlag;
                StringValue = param.ToString();
                NestedModel = _nested ?? new NestedCommandViewModel();
            });

            Task.Run(() =>
            {
                while (true)
                {
                    CurrentTime = DateTimeOffset.Now.ToString();
                    Thread.Sleep(1000);
                }
            });

            CurrentTimeObservable = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(1))
                .Select(x => DateTimeOffset.Now.ToString());
        }

        public ObservableCollection<TestItem> Items { get; }
        public ObservableCollection<TestItem> SelectedItems { get; }
        public ReactiveCommand<Unit, Unit> ShuffleItems { get; }

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

        public string CurrentTime
        {
            get { return _currentTime; }
            private set { this.RaiseAndSetIfChanged(ref _currentTime, value); }
        }

        public IObservable<string> CurrentTimeObservable { get; }
        public ReactiveCommand<object, Unit> StringValueCommand { get; }

        public DataAnnotationsErrorViewModel DataAnnotationsValidation { get; } = new DataAnnotationsErrorViewModel();
        public ExceptionErrorViewModel ExceptionDataValidation { get; } = new ExceptionErrorViewModel();
        public IndeiErrorViewModel IndeiDataValidation { get; } = new IndeiErrorViewModel();

        public NestedCommandViewModel NestedModel
        {
            get { return _nested; }
            private set { this.RaiseAndSetIfChanged(ref _nested, value); }
        }
    }
}
