using ReactiveUI;
using System;
using System.IO;

namespace ControlCatalog
{
    public class RecentProjectViewModel : ReactiveObject
    {
        public RecentProjectViewModel(string name, string location)
        {
            _name = name;
            _location = location;

            ClickCommand = ReactiveCommand.Create(() =>
            {
                Console.WriteLine("Recent Project Clicked");
            });
        }

        private string _name;

        public string Name
        {
            get { return _name; }
            set { this.RaiseAndSetIfChanged(ref _name, value); }
        }

        private string _location;

        public string Location
        {
            get { return _location; }
            set { this.RaiseAndSetIfChanged(ref _location, value); }
        }

        public ReactiveCommand ClickCommand { get; }
    }
}