using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;

namespace ControlCatalog.Pages
{
    public partial class ClipboardPage : UserControl
    {
        private INotificationManager? _notificationManager;
        private INotificationManager NotificationManager => _notificationManager
            ??= new WindowNotificationManager(TopLevel.GetTopLevel(this)!);
        public ClipboardPage()
        {
            InitializeComponent();
        }

        private TextBox ClipboardContent => this.Get<TextBox>("ClipboardContent");

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void CopyText(object? sender, RoutedEventArgs args)
        {
            if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard && ClipboardContent is { } clipboardContent)
                await clipboard.SetTextAsync(clipboardContent.Text ?? String.Empty);
        }

        private async void PasteText(object? sender, RoutedEventArgs args)
        {
            if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
            {
                ClipboardContent.Text = await clipboard.GetTextAsync();
            }
        }

        private async void CopyTextDataObject(object? sender, RoutedEventArgs args)
        {
            if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
            {
                var dataObject = new DataObject();
                dataObject.Set(DataFormats.Text, ClipboardContent.Text ?? string.Empty);
                await clipboard.SetDataObjectAsync(dataObject);
            }
        }

        private async void PasteTextDataObject(object? sender, RoutedEventArgs args)
        {
            if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
            {
                ClipboardContent.Text = await clipboard.GetDataAsync(DataFormats.Text) as string ?? string.Empty;
            }
        }

        private async void CopyFilesDataObject(object? sender, RoutedEventArgs args)
        {
            if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
            {
                var storageProvider = TopLevel.GetTopLevel(this)!.StorageProvider;
                var filesPath = (ClipboardContent.Text ?? string.Empty)
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                if (filesPath.Length == 0)
                {
                    return;
                }
                List<string> invalidFile = new(filesPath.Length);
                List<IStorageFile> files = new(filesPath.Length);

                for (int i = 0; i < filesPath.Length; i++)
                {
                    var file = await storageProvider.TryGetFileFromPathAsync(filesPath[i]);
                    if (file is null)
                    {
                        invalidFile.Add(filesPath[i]);
                    }
                    else
                    {
                        files.Add(file);
                    }
                }

                if (invalidFile.Count > 0)
                {
                    NotificationManager.Show(new Notification("Warning", "There is one o more invalid path.", NotificationType.Warning));
                }

                if (files.Count > 0)
                {
                    var dataObject = new DataObject();
                    dataObject.Set(DataFormats.Files, files);
                    await clipboard.SetDataObjectAsync(dataObject);
                    NotificationManager.Show(new Notification("Success", "Copy completated.", NotificationType.Success));
                }
                else
                {
                    NotificationManager.Show(new Notification("Warning", "Any files to copy in Clipboard.", NotificationType.Warning));
                }
            }
        }

        private async void PasteFilesDataObject(object? sender, RoutedEventArgs args)
        {
            if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
            {
                var files = await clipboard.GetDataAsync(DataFormats.Files) as IEnumerable<Avalonia.Platform.Storage.IStorageItem>;

                ClipboardContent.Text = files != null ? string.Join(Environment.NewLine, files.Select(f => f.TryGetLocalPath() ?? f.Name)) : string.Empty;
            }
        }

        private async void GetFormats(object sender, RoutedEventArgs args)
        {
            if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
            {
                var formats = await clipboard.GetFormatsAsync();
                ClipboardContent.Text = string.Join(Environment.NewLine, formats);
            }
        }

        private async void Clear(object sender, RoutedEventArgs args)
        {
            if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
            {
                await clipboard.ClearAsync();
            }

        }
    }
}
