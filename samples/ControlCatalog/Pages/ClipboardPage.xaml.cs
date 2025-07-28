using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Notifications;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace ControlCatalog.Pages
{
    public partial class ClipboardPage : UserControl
    {
        private readonly DataFormat _customBinaryDataFormat =
            DataFormat.CreateApplicationFormat("controlcatalog-binary-data");

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
                        dataTransfer.Add(DataFormat.File, file);
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
                ClipboardContent.Text = string.Join(Environment.NewLine, formats);
            }
        }

        private async void CopyBinaryData(object? sender, RoutedEventArgs args)
        {
            if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
            {
                var dataTransfer = _storedDataTransfer = new DataTransfer();
                var bytes = new byte[10 * 1024 * 1024];
                new Random().NextBytes(bytes);
                dataTransfer.Add(_customBinaryDataFormat, bytes);
                await clipboard.SetDataAsync(dataTransfer);
            }
        }

        private async void PasteBinaryData(object? sender, RoutedEventArgs args)
        {
            if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
            {
                var bytes = await clipboard.TryGetValueAsync<byte[]>(_customBinaryDataFormat);
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
                    var dataTransfer = await clipboard.TryGetInProcessDataAsync();
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
