using System;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public partial class ClipboardPage : UserControl
    {
        public ClipboardPage()
        {
            InitializeComponent();
        }

        private TextBox ClipboardContent => this.Get<TextBox>("ClipboardContent");

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void CopyText(object sender, RoutedEventArgs args)
        {
            await Application.Current.Clipboard.SetTextAsync(ClipboardContent.Text);
        }

        private async void PasteText(object sender, RoutedEventArgs args)
        {
            ClipboardContent.Text = await Application.Current.Clipboard.GetTextAsync();
        }

        private async void CopyTextDataObject(object sender, RoutedEventArgs args)
        {
            var dataObject = new DataObject();
            dataObject.Set(DataFormats.Text, ClipboardContent.Text ?? string.Empty);
            await Application.Current.Clipboard.SetDataObjectAsync(dataObject);
        }

        private async void PasteTextDataObject(object sender, RoutedEventArgs args)
        {
            ClipboardContent.Text = await Application.Current.Clipboard.GetDataAsync(DataFormats.Text) as string ?? string.Empty;
        }

        private async void CopyFilesDataObject(object sender, RoutedEventArgs args)
        {
            var files = ClipboardContent.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            if (files.Length == 0)
            {
                return;
            }
            var dataObject = new DataObject();
            dataObject.Set(DataFormats.FileNames, files);
            await Application.Current.Clipboard.SetDataObjectAsync(dataObject);
        }

        private async void PasteFilesDataObject(object sender, RoutedEventArgs args)
        {
            var fiels = await Application.Current.Clipboard.GetDataAsync(DataFormats.FileNames) as IEnumerable<string>;
            ClipboardContent.Text = fiels != null ? string.Join(Environment.NewLine, fiels) : string.Empty;
        }

        private async void GetFormats(object sender, RoutedEventArgs args)
        {
            var formats = await Application.Current.Clipboard.GetFormatsAsync();
            ClipboardContent.Text = string.Join(Environment.NewLine, formats);
        }

        private async void Clear(object sender, RoutedEventArgs args)
        {
            await Application.Current.Clipboard.ClearAsync();
        }
    }
}
