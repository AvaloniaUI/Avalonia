using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Metadata;
using ReactiveUI;

namespace BindingDemo.ViewModels
{
    public class DownloadViewModel: ReactiveObject
    {
        bool _isBusy;

        public bool IsBusy
        {
            get { return _isBusy; }
            private set { this.RaiseAndSetIfChanged(ref _isBusy, value); }
        }

        double _downloadProgess;

        public double DownloadProgess
        {
            get { return _downloadProgess; }
            private set { this.RaiseAndSetIfChanged(ref _downloadProgess, value); }
        }


        string _source = @"https://github.com/AvaloniaUI/Avalonia.git";
        public string Source
        {
            get { return _source; }
            private set { this.RaiseAndSetIfChanged(ref _source, value); }
        }

        string _destination;
        public string Destination
        {
            get { return _destination; }
            private set { this.RaiseAndSetIfChanged(ref _destination, value); }
        }

        public async Task ChangeDestination()
        {
            var dialog = new OpenFolderDialog()
            {
                Title = "Select folder",
            };
            var main = (App.Current.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime).MainWindow;
            Destination = await dialog.ShowAsync(main);
        }


        [DependsOn(nameof(IsBusy))]
        [DependsOn(nameof(Source))]
        [DependsOn(nameof(Destination))]
        bool CanDownloadAsync(object paramter)
        {
            return IsBusy == false
                && string.IsNullOrWhiteSpace(Source) == false
                && string.IsNullOrWhiteSpace(Destination) == false;
        }

        public ObservableCollection<string> DownloadMessage { get; } 
            = new ObservableCollection<string>();

        public async ValueTask DownloadAsync(string parameter)
        {
            //object parameter = null;
            IsBusy = true;
            var watch = new System.Diagnostics.Stopwatch();
            DownloadMessage.Clear();
            watch.Start();
            DownloadProgess = 0;
            await Task.Delay(3000);
            DownloadProgess = 10;
            DownloadMessage.Insert(0, $"Donwload from {parameter} {DownloadProgess}% Elapsed {watch.ElapsedMilliseconds}ms.");
            await Task.Delay(3000);
            DownloadProgess = 20;
            DownloadMessage.Insert(0, $"Donwload from {parameter} {DownloadProgess}% Elapsed {watch.ElapsedMilliseconds}ms.");
            await Task.Delay(3000);
            DownloadProgess = 30;
            DownloadMessage.Insert(0, $"Donwload from {parameter} {DownloadProgess}% Elapsed {watch.ElapsedMilliseconds}ms.");
            await Task.Delay(3000);
            DownloadProgess = 100;
            DownloadMessage.Insert(0, $"Donwload from {parameter} {DownloadProgess}% Elapsed {watch.ElapsedMilliseconds}ms.");
            IsBusy = false;
        }
    }
}
