using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Dialogs;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
#pragma warning disable 4014
namespace ControlCatalog.Pages
{
    public class DialogsPage : UserControl
    {
        public DialogsPage()
        {
            this.InitializeComponent();

            var results = this.Get<ItemsPresenter>("PickerLastResults");
            var resultsVisible = this.Get<TextBlock>("PickerLastResultsVisible");

            string? lastSelectedDirectory = null;

            List<FileDialogFilter>? GetFilters()
            {
                if (this.Get<CheckBox>("UseFilters").IsChecked != true)
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

            this.Get<Button>("OpenFile").Click += async delegate
            {
                // Almost guaranteed to exist
                var fullPath = Assembly.GetEntryAssembly()?.GetModules().FirstOrDefault()?.FullyQualifiedName;
                var initialFileName = fullPath == null ? null : System.IO.Path.GetFileName(fullPath);
                var initialDirectory = fullPath == null ? null : System.IO.Path.GetDirectoryName(fullPath);

                var result = await new OpenFileDialog()
                {
                    Title = "Open file",
                    Filters = GetFilters(),
                    Directory = initialDirectory,
                    InitialFileName = initialFileName
                }.ShowAsync(GetWindow());
                results.Items = result;
                resultsVisible.IsVisible = result?.Any() == true;
            };
            this.Get<Button>("OpenMultipleFiles").Click += async delegate
            {
                var result = await new OpenFileDialog()
                {
                    Title = "Open multiple files",
                    Filters = GetFilters(),
                    Directory = lastSelectedDirectory,
                    AllowMultiple = true
                }.ShowAsync(GetWindow());
                results.Items = result;
                resultsVisible.IsVisible = result?.Any() == true;
            };
            this.Get<Button>("SaveFile").Click += async delegate
            {
                var result = await new SaveFileDialog()
                {
                    Title = "Save file",
                    Filters = GetFilters(),
                    Directory = lastSelectedDirectory,
                    InitialFileName = "test.txt"
                }.ShowAsync(GetWindow());
                results.Items = new[] { result };
                resultsVisible.IsVisible = result != null;
            };
            this.Get<Button>("SelectFolder").Click += async delegate
            {
                var result = await new OpenFolderDialog()
                {
                    Title = "Select folder",
                    Directory = lastSelectedDirectory,
                }.ShowAsync(GetWindow());

                if (!string.IsNullOrEmpty(result))
                {
                    lastSelectedDirectory = result;
                }

                results.Items = new [] { result };
                resultsVisible.IsVisible = result != null;
            };
            this.Get<Button>("OpenBoth").Click += async delegate
            {
                var result = await new OpenFileDialog()
                {
                    Title = "Select both",
                    Directory = lastSelectedDirectory,
                    AllowMultiple = true
                }.ShowManagedAsync(GetWindow(), new ManagedFileDialogOptions
                {
                    AllowDirectorySelection = true
                });
                results.Items = result;
                resultsVisible.IsVisible = result?.Any() == true;
            };
            this.Get<Button>("DecoratedWindow").Click += delegate
            {
                new DecoratedWindow().Show();
            };
            this.Get<Button>("DecoratedWindowDialog").Click += delegate
            {
                new DecoratedWindow().ShowDialog(GetWindow());
            };
            this.Get<Button>("Dialog").Click += delegate
            {
                var window = CreateSampleWindow();
                window.Height = 200;
                window.ShowDialog(GetWindow());
            };
            this.Get<Button>("DialogNoTaskbar").Click += delegate
            {
                var window = CreateSampleWindow();
                window.Height = 200;
                window.ShowInTaskbar = false;
                window.ShowDialog(GetWindow());
            };
            this.Get<Button>("OwnedWindow").Click += delegate
            {
                var window = CreateSampleWindow();

                window.Show(GetWindow());
            };

            this.Get<Button>("OwnedWindowNoTaskbar").Click += delegate
            {
                var window = CreateSampleWindow();

                window.ShowInTaskbar = false;

                window.Show(GetWindow());
            };
        }

        private Window CreateSampleWindow()
        {
            Button button;
            Button dialogButton;
            
            var window = new Window
            {
                Height = 200,
                Width = 200,
                Content = new StackPanel
                {
                    Spacing = 4,
                    Children =
                    {
                        new TextBlock { Text = "Hello world!" },
                        (button = new Button
                        {
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Content = "Click to close",
                            IsDefault = true
                        }),
                        (dialogButton = new Button
                        {
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Content = "Dialog",
                            IsDefault = false
                        })
                    }
                },
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            button.Click += (_, __) => window.Close();
            dialogButton.Click += (_, __) =>
            {
                var dialog = CreateSampleWindow();
                dialog.Height = 200;
                dialog.ShowDialog(window);
            };

            return window;
        }

        Window GetWindow() => this.VisualRoot as Window  ?? throw new NullReferenceException("Invalid Owner");

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
