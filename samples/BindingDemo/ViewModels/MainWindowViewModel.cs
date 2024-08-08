using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using MiniMvvm;
using Avalonia.Controls;
using Avalonia.Metadata;
using Avalonia.Controls.Selection;
using Avalonia.Threading;

namespace BindingDemo.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
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

            Selection = new SelectionModel<TestItem> { SingleSelect = false };

            ShuffleItems = MiniCommand.Create(() =>
            {
                var r = new Random();
                Items.Move(r.Next(Items.Count), 1);
            });

            StringValueCommand = MiniCommand.Create<object>(param =>
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

            CurrentTimeObservable = new DispatcherTimerObservable(TimeSpan.FromMilliseconds(500));
        }

        public ObservableCollection<TestItem> Items { get; }
        public SelectionModel<TestItem> Selection { get; }
        public MiniCommand ShuffleItems { get; }

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

        public IObservable<DateTimeOffset> CurrentTimeObservable { get; }
        public MiniCommand StringValueCommand { get; }

        public DataAnnotationsErrorViewModel DataAnnotationsValidation { get; } = new DataAnnotationsErrorViewModel();
        public ExceptionErrorViewModel ExceptionDataValidation { get; } = new ExceptionErrorViewModel();
        public IndeiErrorViewModel IndeiDataValidation { get; } = new IndeiErrorViewModel();

        public NestedCommandViewModel NestedModel
        {
            get { return _nested; }
            private set { this.RaiseAndSetIfChanged(ref _nested, value); }
        }

        public void Do(object parameter)
        {

        }

        [DependsOn(nameof(BooleanFlag))]
        bool CanDo(object parameter)
        {
            return BooleanFlag;
        }

        private class DispatcherTimerObservable(TimeSpan interval) : IObservable<DateTimeOffset>
        {
            public IDisposable Subscribe(IObserver<DateTimeOffset> observer)
            {
                var timer = new DispatcherTimer();
                timer.Tag = observer;
                timer.Interval = interval;
                timer.Tick += static (s, _) =>
                {
                    var observer = (IObserver<DateTimeOffset>)((DispatcherTimer)s!).Tag!;
                    observer.OnNext(DateTimeOffset.Now);
                };
                timer.Start();
                return new Disposable(timer, observer);
            }

            private class Disposable(DispatcherTimer timer, IObserver<DateTimeOffset> observer) : IDisposable
            {
                public void Dispose()
                {
                    timer.Stop();
                    observer.OnCompleted();
                }
            }
        }
    }
}
