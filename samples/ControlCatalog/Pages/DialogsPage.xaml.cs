using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Dialogs;
using Avalonia.Markup.Xaml;
#pragma warning disable 4014

namespace ControlCatalog.Pages
{
    public class DialogsPage : UserControl
    {
        public DialogsPage()
        {
            this.InitializeComponent();

            List<FileDialogFilter> GetFilters()
            {
                if (this.FindControl<CheckBox>("UseFilters").IsChecked != true)
                    return null;
                return  new List<FileDialogFilter>
                {
                    new FileDialogFilter
                    {
                        Name = "Text files (.txt)", Extensions = new List<string> {"txt"}
                    },
                    new FileDialogFilter
                    {
                        Name = "All files",
                        Extensions = new List<string> {"*"}
                    }
                };
            }

            this.FindControl<Button>("OpenFile").Click += delegate
            {
                new OpenFileDialog()
                {
                    Title = "Open file",
                    Filters = GetFilters(),
                    // Almost guaranteed to exist
                    InitialFileName = Assembly.GetEntryAssembly()?.GetModules().FirstOrDefault()?.FullyQualifiedName
                }.ShowAsync(GetWindow());
            };
            this.FindControl<Button>("SaveFile").Click += delegate
            {
                new SaveFileDialog()
                {
                    Title = "Save file",
                    Filters = GetFilters(),
                    InitialFileName = "test.txt"
                }.ShowAsync(GetWindow());
            };
            this.FindControl<Button>("SelectFolder").Click += delegate
            {
                new OpenFolderDialog()
                {
                    Title = "Select folder",
                }.ShowAsync(GetWindow());
            };
            this.FindControl<Button>("OpenBoth").Click += async delegate
            {
                var res = await new OpenFileDialog()
                {
                    Title = "Select both",
                    AllowMultiple = true
                }.ShowManagedAsync(GetWindow(), new ManagedFileDialogOptions
                {
                    AllowDirectorySelection = true
                });
                if (res != null)
                    Console.WriteLine("Selected: \n" + string.Join("\n", res));
            };
            this.FindControl<Button>("DecoratedWindow").Click += delegate
            {
                new DecoratedWindow().Show();
            };
            this.FindControl<Button>("DecoratedWindowDialog").Click += delegate
            {
                new DecoratedWindow().ShowDialog(GetWindow());
            };
            this.FindControl<Button>("Dialog").Click += delegate
            {
                var window = CreateSampleWindow();
                window.Height = 200;
                window.ShowDialog(GetWindow());
            };
            this.FindControl<Button>("DialogNoTaskbar").Click += delegate
            {
                var window = CreateSampleWindow();
                window.Height = 200;
                window.ShowInTaskbar = false;
                window.ShowDialog(GetWindow());
            };
            this.FindControl<Button>("OwnedWindow").Click += delegate
            {
                var window = CreateSampleWindow();

                window.Show(GetWindow());
            };

            this.FindControl<Button>("OwnedWindowNoTaskbar").Click += delegate
            {
                var window = CreateSampleWindow();

                window.ShowInTaskbar = false;

                window.Show(GetWindow());
            };
        }

        private Window CreateSampleWindow()
        {
            var window = new Window();
            window.Height = 200;
            window.Width = 200;
            window.Content = new TextBlock { Text = "Hello world!" };
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            return window;
        }

        Window GetWindow() => (Window)this.VisualRoot;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
