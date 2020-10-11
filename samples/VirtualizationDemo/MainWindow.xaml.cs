using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using VirtualizationDemo.ViewModels;

namespace VirtualizationDemo
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.AttachDevTools();
            DataContext = new MainWindowViewModel();
            //bug in listbox
            var items = new AvaloniaList<string>() { "Item 1" };
            var lst = this.Get<ListBox>("lst");
            lst.Items = items;
            this.Get<Button>("btnAdd").Click += (s, e) =>
            {
                items.Add($"Item {items.Count + 1}");
                lst.SelectedIndex = items.Count - 1;
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
