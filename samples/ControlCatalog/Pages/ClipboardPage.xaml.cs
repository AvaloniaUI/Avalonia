using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using Avalonia.Threading;

namespace ControlCatalog.Pages
{
    public partial class ClipboardPage : UserControl
    {
        private INotificationManager? _notificationManager;
        private INotificationManager NotificationManager => _notificationManager
            ??= new WindowNotificationManager(TopLevel.GetTopLevel(this)!);

        private readonly DispatcherTimer _clipboardLastDataObjectChecker;
        private DataTransfer? _storedDataTransfer;
        public ClipboardPage()
        {
            _clipboardLastDataObjectChecker =
                new DispatcherTimer(TimeSpan.FromSeconds(0.5), default, CheckLastDataObject);
            InitializeComponent();
        }

        private TextBox ClipboardContent => this.Get<TextBox>("ClipboardContent");

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void CopyText(object? sender, RoutedEventArgs args)
        {
            if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
                await clipboard.SetTextAsync(ClipboardContent.Text ?? string.Empty);
        }

        private async void PasteText(object? sender, RoutedEventArgs args)
        {
            if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
            {
                ClipboardContent.Text = await clipboard.TryGetTextAsync();
            }
        }

        private async void CopyFiles(object? sender, RoutedEventArgs args)
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
                    var dataTransfer = _storedDataTransfer = new DataTransfer();
                    foreach (var file in files)
                        dataTransfer.Items.Add(DataTransferItem.Create(DataFormat.File, file));
                    await clipboard.SetDataAsync(dataTransfer);
                    NotificationManager.Show(new Notification("Success", "Copy completed.", NotificationType.Success));
                }
                else
                {
                    NotificationManager.Show(new Notification("Warning", "Any files to copy in Clipboard.", NotificationType.Warning));
                }
            }
        }

        private async void PasteFiles(object? sender, RoutedEventArgs args)
        {
            if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
            {
                var files = await clipboard.TryGetFilesAsync();

                ClipboardContent.Text = files != null ? string.Join(Environment.NewLine, files.Select(f => f.TryGetLocalPath() ?? f.Name)) : string.Empty;
            }
        }

        private async void GetFormats(object sender, RoutedEventArgs args)
        {
            if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
            {
                var formats = await clipboard.GetDataFormatsAsync();
                ClipboardContent.Text = string.Join<DataFormat>(Environment.NewLine, formats);
            }
        }

        private async void CopyBinaryData(object? sender, RoutedEventArgs args)
        {
            if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
            {
                var dataTransfer = _storedDataTransfer = new DataTransfer();
                var bytes = Convert.FromBase64String(
                     "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");
                dataTransfer.Items.Add(DataTransferItem.Create(DataFormat.CreateOperatingSystemFormat("image/png"), bytes));
                await clipboard.SetDataAsync(dataTransfer);
            }
        }

        private async void PasteBinaryData(object? sender, RoutedEventArgs args)
        {
            if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
            {
                var bytes = await clipboard.TryGetValueAsync<byte[]>(DataFormat.CreateOperatingSystemFormat("image/png"));
                ClipboardContent.Text = bytes is null ? "<null>" : $"{bytes.Length} bytes";
            }
        }

        private async void Clear(object sender, RoutedEventArgs args)
        {
            if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
            {
                await clipboard.ClearAsync();
            }

        }


        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _clipboardLastDataObjectChecker.Start();
            base.OnAttachedToVisualTree(e);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _clipboardLastDataObjectChecker.Stop();
            base.OnDetachedFromVisualTree(e);
        }

        private Run OwnsClipboardDataObject => this.Get<Run>("OwnsClipboardDataObject");
        private bool _checkingClipboardDataTransfer;
        private async void CheckLastDataObject(object? sender, EventArgs e)
        {
            if(_checkingClipboardDataTransfer)
                return;
            try
            {
                _checkingClipboardDataTransfer = true;

                var owns = false;
                if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
                {
                    var dataTransfer = await clipboard.TryGetInProcessDataTransferAsync();
                    owns = dataTransfer == _storedDataTransfer && dataTransfer is not null;
                }

                OwnsClipboardDataObject.Text = owns ? "Yes" : "No";
                OwnsClipboardDataObject.Foreground = owns ? Brushes.Green : Brushes.Red;
            }
            finally
            {
                _checkingClipboardDataTransfer = false;
            }
        }
    }
}
