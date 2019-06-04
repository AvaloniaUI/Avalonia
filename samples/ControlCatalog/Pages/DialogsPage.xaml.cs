using System.Collections.Generic;
using Avalonia.Controls;
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
                    Filters = GetFilters()
                }.ShowAsync(GetWindow());
            };
            this.FindControl<Button>("SaveFile").Click += delegate
            {
                new SaveFileDialog()
                {
                    Title = "Save file",
                    Filters = GetFilters()
                }.ShowAsync(GetWindow());
            };
            this.FindControl<Button>("SelectFolder").Click += delegate
            {
                new OpenFolderDialog()
                {
                    Title = "Select folder"
                }.ShowAsync(GetWindow());
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
                    var window = new Window();
                    window.Height = 200;
                    window.Width = 200;
                    window.Content = new TextBlock { Text = "Hello world!" };
                    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    window.ShowDialog(GetWindow());
                };
        }

        Window GetWindow() => (Window)this.VisualRoot;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
