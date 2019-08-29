using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Triangle.Models;
using DynamicData;
using ReactiveUI;

namespace Avalonia.Triangle.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private Dot _startDot = new Dot();
        private Dot _firstDot = new Dot();
        private Dot _secondDot = new Dot();
        private Dot _thirdDot = new Dot();
        private bool stat;
        private object locker = new object();

        public ReactiveCommand<Unit, Unit> StartCommand { get; }
        public int StartDotX
        {
            get => (int) _startDot.X;
            set => this.RaiseAndSetIfChanged(ref _startDot.X, value);
        }

        public int StartDotY
        {
            get => (int) _startDot.Y;
            set => this.RaiseAndSetIfChanged(ref _startDot.Y, value);
        }

        public int FirstDotX
        {
            get => (int) _firstDot.X;
            set => this.RaiseAndSetIfChanged(ref _firstDot.X, value);
        }

        public int FirstDotY
        {
            get => (int) _firstDot.Y;
            set => this.RaiseAndSetIfChanged(ref _firstDot.Y, value);
        }

        public int SecondDotX
        {
            get => (int) _secondDot.X;
            set => this.RaiseAndSetIfChanged(ref _secondDot.X, value);
        }

        public int SecondDotY
        {
            get => (int) _secondDot.Y;
            set => this.RaiseAndSetIfChanged(ref _secondDot.Y, value);
        }

        public int ThirdDotX
        {
            get => (int) _thirdDot.X;
            set => this.RaiseAndSetIfChanged(ref _thirdDot.X, value);
        }

        public int ThirdDotY
        {
            get => (int) _thirdDot.Y;
            set => this.RaiseAndSetIfChanged(ref _thirdDot.Y, value);
        }

        SourceList<DotObject> dots = new SourceList<DotObject>();
        private ReadOnlyObservableCollection<DotObject> _collection;
        public ReadOnlyObservableCollection<DotObject> Collection => _collection;

        public MainWindowViewModel()
        {
            StartCommand = ReactiveCommand.CreateFromTask(Start);
            _firstDot = new Dot {X = 250, Y = 50};
            _secondDot = new Dot {X = 50, Y = 500};
            _thirdDot = new Dot {X = 500, Y = 500};
            dots.Connect().ObserveOn(RxApp.MainThreadScheduler).Bind(out _collection).Subscribe();
        }

        public void SetDots()
        {
            stat = false;
            dots.Clear();
            dots.Add(new DotObject(_firstDot));
            dots.Add(new DotObject(_secondDot));
            dots.Add(new DotObject(_thirdDot));
            dots.Add(new DotObject(_startDot));
        }

        public void Clear()
        {
            stat = false;
            dots.Clear();
        }

        public async Task Start()
        {
            stat = true;
            var commonDot = _startDot;
            await Task.Run(async () =>
            {
                while (stat)
                {
                    var rnd = new Random().Next(1, 4);
                    switch (rnd)
                    {
                        case 1:
                        {
                            commonDot.X = (_firstDot.X + commonDot.X) / 2;
                            commonDot.Y = (_firstDot.Y + commonDot.Y) / 2;
                            break;
                        }
                        case 2:
                        {
                            commonDot.X = (_secondDot.X + commonDot.X) / 2;
                            commonDot.Y = (_secondDot.Y + commonDot.Y) / 2;
                            break;
                        }
                        case 3:
                        {
                            commonDot.X = (_thirdDot.X + commonDot.X) / 2;
                            commonDot.Y = (_thirdDot.Y + commonDot.Y) / 2;
                            break;
                        }
                    }

                    //SourceList<DotObject> dots = new SourceList<DotObject>();
                   
                        dots.Add(new DotObject(commonDot));
                        Console.WriteLine($"+1 {dots.Count}");

                    await Task.Delay(10);
                }
            });

                
            }
        


        public void Stop()
        {
            stat = false;
        }
    }
}