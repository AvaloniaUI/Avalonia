using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.VisualTree;
using ReactiveUI;

namespace ControlCatalog.ViewModels
{
    public class ContextMenuPageViewModel
    {
        public Control View { get; set; }
        public ContextMenuPageViewModel()
        {
            OpenCommand = ReactiveCommand.CreateFromTask(Open);
            SaveCommand = ReactiveCommand.Create(Save);
            OpenRecentCommand = ReactiveCommand.Create<string>(OpenRecent);

            MenuItems = new[]
            {
                new MenuItemViewModel { Header = "_Open...", Command = OpenCommand },
                new MenuItemViewModel { Header = "Save", Command = SaveCommand },
                new MenuItemViewModel { Header = "-" },
                new MenuItemViewModel
                {
                    Header = "Recent",
                    Items = new[]
                    {
                        new MenuItemViewModel
                        {
                            Header = "File1.txt",
                            Command = OpenRecentCommand,
                            CommandParameter = @"c:\foo\File1.txt"
                        },
                        new MenuItemViewModel
                        {
                            Header = "File2.txt",
                            Command = OpenRecentCommand,
                            CommandParameter = @"c:\foo\File2.txt"
                        },
                    }
                },
            };
        }

        public IReadOnlyList<MenuItemViewModel> MenuItems { get; set; }
        public ReactiveCommand<Unit, Unit> OpenCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveCommand { get; }
        public ReactiveCommand<string, Unit> OpenRecentCommand { get; }

        public async Task Open()
        {
            var window = View?.GetVisualRoot() as Window;
            if (window == null)
                return;
            var dialog = new OpenFileDialog();
            var result = await dialog.ShowAsync(window);

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
}
