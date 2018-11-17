using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;

namespace ControlCatalog.Pages
{
    public class MenuPage : UserControl
    {
        public MenuPage()
        {
            this.InitializeComponent();
            var vm = new MenuPageViewModel();

            vm.MenuItems = new[]
            {
                new MenuItemViewModel
                {
                    Header = "_File",
                    Items = new[]
                    {
                        new MenuItemViewModel { Header = "_Open...", Command = vm.OpenCommand },
                        new MenuItemViewModel { Header = "Save", Command = vm.SaveCommand },
                        new MenuItemViewModel { Header = "-" },
                        new MenuItemViewModel
                        {
                            Header = "Recent",
                            Items = new[]
                            {
                                new MenuItemViewModel
                                {
                                    Header = "File1.txt",
                                    Command = vm.OpenRecentCommand,
                                    CommandParameter = @"c:\foo\File1.txt"
                                },
                                new MenuItemViewModel
                                {
                                    Header = "File2.txt",
                                    Command = vm.OpenRecentCommand,
                                    CommandParameter = @"c:\foo\File2.txt"
                                },
                            }
                        },
                    }
                },
                new MenuItemViewModel
                {
                    Header = "_Edit",
                    Items = new[]
                    {
                        new MenuItemViewModel { Header = "_Copy" },
                        new MenuItemViewModel { Header = "_Paste" },
                    }
                }
            };

            DataContext = vm;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }

    public class MenuPageViewModel
    {
        public MenuPageViewModel()
        {
            OpenCommand = ReactiveCommand.CreateFromTask(Open);
            SaveCommand = ReactiveCommand.Create(Save);
            OpenRecentCommand = ReactiveCommand.Create<string>(OpenRecent);
        }

        public IReadOnlyList<MenuItemViewModel> MenuItems { get; set; }
        public ReactiveCommand<Unit, Unit> OpenCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveCommand { get; }
        public ReactiveCommand<string, Unit> OpenRecentCommand { get; }

        public async Task Open()
        {
            var dialog = new OpenFileDialog();
            var result = await dialog.ShowAsync(App.Current.MainWindow);

            if (result != null)
            {
                foreach (var path in result)
                {
                    System.Diagnostics.Debug.WriteLine($"Opened: {path}");
                }
            }
        }

        public void Save()
        {
            System.Diagnostics.Debug.WriteLine("Save");
        }

        public void OpenRecent(string path)
        {
            System.Diagnostics.Debug.WriteLine($"Open recent: {path}");
        }
    }

    public class MenuItemViewModel
    {
        public string Header { get; set; }
        public ICommand Command { get; set; }
        public object CommandParameter { get; set; }
        public IList<MenuItemViewModel> Items { get; set; }
    }
}
